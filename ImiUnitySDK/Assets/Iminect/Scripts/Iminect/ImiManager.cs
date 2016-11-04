//***********************************************************************************************************************
//
//文件名(File Name):     IminectPlayerInfo .cs
//
//功能描述(Description):    体感数据处理
//
//作者(Author):		郭佳
//
//日期(Create Date):	
//
//修改记录(Revision History):
//			R1：
//				修改作者:
//				修改日期:
//				修改理由:				
//
//***********************************************************************************************************************
using UnityEngine;
using System;
using System.Threading;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using IMIForUnity;

namespace IMIForUnity
{

    public class ImiManager : MonoBehaviour
    {
        public enum ErrorCode
        {
            OK,
            ERROR
        }

        private static ImiManager instance = null;
        private static ImiManagerImp imiManagerImp = null;


        //Depth, Skeleton, Depth + Skeleton, User Depth + Skeleton
        public bool processDepth = false;
        public bool processSkeleton = false;
        public bool processDepthSkeleton = false;
        public bool processUserDepthAndSkeleton = true;

        //Color Stream
        public bool processColorData = false;



        /// <summary>
        /// Action Queue for executing in main thread
        /// </summary>
        private readonly static Queue<Action> _executionQueue = new Queue<Action>();


#if UNITY_ANDROID
        private int fd = -1;
        private string usbPath;
        private AndroidJavaObject activityContext = null;
        private AndroidJavaObject helper = null;
        private OnPermissionGet permissionCallback;
        /// <summary>
        /// Callback when Android Usb Permission is get/denied
        /// </summary>
        /// <param name="code"></param>
        public delegate void OnPermissionGet(ErrorCode code);
#endif

        public static ImiManager GetInstance()
        {
            if (!Exists())
            {
                throw new Exception("Could not find the ImiManager object. Please ensure you have added the ImiManager Prefab to your scene.");
            }
            return instance;
       }

        private ImiManager() { }


#if UNITY_ANDROID
        public void RequirePermission(OnPermissionGet permissionCallback)
        {
            this.permissionCallback = permissionCallback;
            requirePermission();
        }
#endif

        void Awake()
        {
            if (instance == null)
            {
                instance = this;
                imiManagerImp = ImiManagerImp.GetInstance();
                DontDestroyOnLoad(this.gameObject);
                init();
            }
        }

        void OnDestroy()
        {
            instance = null;
        }


        public void Update()
        {
            lock (_executionQueue)
            {
                while (_executionQueue.Count > 0)
                {
                    _executionQueue.Dequeue().Invoke();
                }
            }
        }

        /// <summary>
        /// Init Device depending on platform
        /// </summary>
        private void init()
        {
#if UNITY_ANDROID
        Debug.Log("Android Platform");
        RequirePermission(delegate (ImiManager.ErrorCode code)
        {
            if (code == ImiManager.ErrorCode.OK)
            {
                Debug.Log("Permission Granted, Trying to initialize device");
                imiManagerImp.init();
            }
            else
            {
                Debug.LogError("Device Inited failed! Maybe permission is denied!");
            }
        });
#elif UNITY_EDITOR
        Debug.Log("Editor");
        imiManagerImp.init();
#else
        Debug.Log("Other Platform");
         imiManagerImp.init();
#endif
        }

        public bool IsInitialized()
        {
            return imiManagerImp.iminectInitialized;
        }

        public Texture2D GetUserColorTexture()
        {
            return imiManagerImp.userClrTex;
        }

        public Texture2D GetDepthTexture()
        {
            return imiManagerImp.usersLblTex;
        }

        public Dictionary<uint, ImiPlayerInfo> GetPlayerInfos()
        {
            return imiManagerImp.playerInfos;
        }

        /// <summary>
        /// Locks the queue and adds the IEnumerator to the queue
        /// </summary>
        /// <param name="action">IEnumerator function that will be executed from the main thread.</param>
        public void Enqueue(IEnumerator action)
        {
            lock (_executionQueue)
            {
                _executionQueue.Enqueue(() =>
                {
                    StartCoroutine(action);
                });
            }
        }

        /// <summary>
        /// Locks the queue and adds the Action to the queue
        /// </summary>
        /// <param name="action">function that will be executed from the main thread.</param>
        public void Enqueue(Action action)
        {
            Enqueue(ActionWrapper(action));
        }
        IEnumerator ActionWrapper(Action a)
        {
            a();
            yield return null;
        }

        public static bool Exists()
        {
            return instance != null;
        }



        public ImiWrapper.ErrorCode OpenDevice()
        {

            ImiWrapper.ErrorCode error;
#if UNITY_ANDROID
            //return if the device has not been opened
            if (fd == -1)
            {
                return ImiWrapper.ErrorCode.OPEN_DEVICE_FAILED;
            }

            if (fd == 0)
            {
                //open device on rooted system
                if ((error = ImiWrapper.OpenDevice()) != ImiWrapper.ErrorCode.OK)
                {
                    Debug.LogError(error);
                    return ImiWrapper.ErrorCode.OPEN_DEVICE_FAILED;
                }
            }
            else
            {
                if ((error = ImiWrapper.OpenDevice2(fd, usbPath)) != ImiWrapper.ErrorCode.OK)
                {
                    Debug.LogError(error);
                    return ImiWrapper.ErrorCode.OPEN_DEVICE_FAILED;
                }

            }
#else
            //open device on rooted system
            if ((error = ImiWrapper.OpenDevice()) != ImiWrapper.ErrorCode.OK)
                {
                    Debug.LogError(error);
                    return ImiWrapper.ErrorCode.OPEN_DEVICE_FAILED;
                }
#endif
            return ImiWrapper.ErrorCode.OK;
        }


#if UNITY_ANDROID
        /**
         * Permission Callback from ImiUsbPermissionHelper
         **/
        private class PermissionCallback : AndroidJavaProxy
        {
            public PermissionCallback() : base("com.hjimi.imipm.ImiUsbPermissionHelper$Callback")
            {
            }

            /**
             *  Called when get usb permission
             *  
             * _fd : fileDescriptor
             * device: UsbDevice
             *
            **/
            void onPermissionGranted(int _fd, AndroidJavaObject device)
            {
                if (instance == null)
                {
                    return;
                }
                instance.fd = _fd;
                if (device != null)
                {
                    instance.usbPath = device.Call<string>("getDeviceName");
                }
                instance.Enqueue(instance.ExecutedOnTheMainThread(ErrorCode.OK));
                Debug.Log("Permission Granted!" + "fd = " + instance.fd + "path = " + instance.usbPath);

            }

            /**
             * 
             * Get Usb Permission failed
             * 
             **/
            void onPermissionDenied()
            {
                if (instance == null)
                {
                    return;
                }
                instance.Enqueue(instance.ExecutedOnTheMainThread(ErrorCode.ERROR));

                Debug.LogError("On Permission Denied!");
            }
        }

        public IEnumerator ExecutedOnTheMainThread(ErrorCode code)
        {
            if (instance.permissionCallback != null)
            {
                instance.permissionCallback(code);
            }
            yield return null;
        }



        private void requirePermission()
        {

            using (AndroidJavaClass activityClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                activityContext = activityClass.GetStatic<AndroidJavaObject>("currentActivity");
            }
            using (AndroidJavaClass helperClass = new AndroidJavaClass("com.hjimi.imipm.ImiUsbPermissionHelper"))
            {
                if (helperClass != null)
                {
                    helper = helperClass.CallStatic<AndroidJavaObject>("newInstance", activityContext);
                    helper.Call("requestPermission", new PermissionCallback());
                }
            }

        }
        /**
         * Maximize the performance of devices
         */
        public void EnforcePerformance()
        {
            if (helper == null)
            {
                Debug.LogError("PermissionHelper is Null!");
                return;
            }

            helper.Call("enforcePerformance");
        }
        /**
         * This method get usb permission automatically on rooted devices
         * by executing chmod 777 /dev/bus/usb/*
         */
        public void EnforcePermission()
        {
            if (helper == null)
            {
                Debug.LogError("PermissionHelper is Null!");
                return;
            }

            helper.Call("enforcePermission");

        }
#endif

        public bool IsDeviceAvailable()
        {
#if UNITY_ANDROID
            return fd != -1;
#else
            return true;
#endif
        }


        /**
         * TODO support for hot swap
         */
        private void CheckDeviceState()
        {
            if (ImiWrapper.GetDeviceState() == ImiWrapper.ImiDeviceState.IMI_DEVICE_STATE_DISCONNECT)
            {
                Debug.LogError("IMI_DEVICE_STATE_DISCONNECT");

            }

            if (ImiWrapper.GetDeviceState() == ImiWrapper.ImiDeviceState.IMI_DEVICE_STATE_CONNECT)
            {
                Debug.Log("IMI_DEVICE_STATE_CONNECT");
            }
        }
    }
}
