using System.Collections;
using UnityEngine;
using System;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using IMIForUnity;


namespace IMIForUnity
{

    public class ImiManagerImp
    {

        private const int USER_NUM = 10;
        private const int THREAD_INTERVAL = 1000 / 60;

        //体感设备是否初始化成功.
        [HideInInspector]
        public bool iminectInitialized = false;

        /// <summary>
        /// 所有玩家体感数据存储数组.
        /// </summary>
        public Dictionary<uint, ImiPlayerInfo> playerInfos;

        [HideInInspector]
        public Texture2D userClrTex;
        /// <summary>
        /// 所有玩家前景texture.
        /// </summary>
        [HideInInspector]
        public Texture2D usersLblTex;

        /// <summary>
        /// 深度数据.
        /// </summary>
        private ushort[] depthData;

        /// <summary>
        /// 包含玩家信息的深度数据.
        /// </summary>
        private ushort[] depthPlayerData;

        /// <summary>
        /// 彩色数据.
        /// </summary>
        [HideInInspector]
        public byte[] colorData;

        /// <summary>
        /// 所有玩家彩色数据.
        /// </summary>
        private byte[] usersDepthColors;

        /// <summary>
        /// 玩家深度直方图数据.
        /// </summary>
        private float[] usersHistogramMap;

        private int depthMapSize = ImiWrapper.DEPTHWIDTH * ImiWrapper.DEPTHHEIGHT;

        private ImiWrapper.ImiSkeletonFrame skeletonFrame;

        private static ImiManagerImp instance;

        private int skeletonJointsCount = 0;

        private bool isPrcStop = false;
        private bool prcDepthDrawLock = false;
        private bool prcColorDrawLock = false;
        private ImiWrapper.ErrorCode retDepth = ImiWrapper.ErrorCode.NONE;
        private ImiWrapper.ErrorCode retColor = ImiWrapper.ErrorCode.NONE;

        private bool isClosed = false;


        /** For java inter calling **/
        private ImiManager imiManager;


        private static object _lock = new object();
        public static ImiManagerImp GetInstance()
        {
            if (instance == null)
            {
                lock (_lock)
                {
                    if(instance == null)
                    {
                        instance = new ImiManagerImp();
                    }
                }
           }
            return instance;
        }
        private ImiManagerImp()
        {
            imiManager = ImiManager.GetInstance();
        }

        public void init()
        {
            InitializeDisplayImageData();
            InitializeIminectData();
            IminectInitialize();
            InitPrcImiDataThd();
        }


        public void DoDestroy()
        {
            Debug.Log("On Destory");
            isPrcStop = true;
            isClosed = true;
#if UNITY_EDITOR
            //if you calling close in editor mode and cause unity to crash
            //Please comment this line and never use it in editor mode
            Close();
#else
        Close();
#endif
        }

        public void Close()
        {
            iminectInitialized = false;
            ImiWrapper.ErrorCode error;

            if (imiManager.processDepth)
            {
                error = ImiWrapper.CloseStream(ImiWrapper.ImiFrameType.IMI_DEPTH_FRAME);
                if (error != ImiWrapper.ErrorCode.OK)
                {
                    Debug.LogError(error);
                }
            }

            if (imiManager.processSkeleton)
            {
                error = ImiWrapper.CloseStream(ImiWrapper.ImiFrameType.IMI_SKELETON_FRAME);
                if (error != ImiWrapper.ErrorCode.OK)
                {
                    Debug.LogError(error);
                }
            }

            if (imiManager.processDepthSkeleton)
            {
                error = ImiWrapper.CloseStream(ImiWrapper.ImiFrameType.IMI_DEPTH_SKELETON_FRAME);
                if (error != ImiWrapper.ErrorCode.OK)
                {
                    Debug.LogError(error);
                }
            }

            if (imiManager.processUserDepthAndSkeleton)
            {
                error = ImiWrapper.CloseStream(ImiWrapper.ImiFrameType.IMI_USER_INDEX_SKELETON_FRAME);
                if (error != ImiWrapper.ErrorCode.OK)
                {
                    Debug.LogError(error);
                }
            }

            if (imiManager.processColorData)
            {
                error = ImiWrapper.CloseStream(ImiWrapper.ImiFrameType.IMI_COLOR_FRAME);
                if (error != ImiWrapper.ErrorCode.OK)
                {
                    Debug.LogError(error);
                }
            }
            error = ImiWrapper.CloseDevice();
            if (error != ImiWrapper.ErrorCode.OK)
            {
                Debug.LogError(error);
            }
        }

        private void InitPrcImiDataThd()
        {
            if (imiManager.processUserDepthAndSkeleton)
            {
                Thread prcUDSThd = new Thread(new ThreadStart(PrcImiDepthAndSkeletonData));
                prcUDSThd.Start();
            }

            if (imiManager.processColorData)
            {
                Thread prcClrThd = new Thread(new ThreadStart(PrcColorData));
                prcClrThd.Start();
            }

            if (imiManager.processDepth)
            {
                Thread prcDThd = new Thread(new ThreadStart(PrcDepthData));
                prcDThd.Start();
            }

            if (imiManager.processSkeleton)
            {
                Thread prcSThd = new Thread(new ThreadStart(PrcSkeletonData));
                prcSThd.Start();
            }

            if (imiManager.processDepthSkeleton)
            {
                Thread prcDSThd = new Thread(new ThreadStart(PrcDepthAndSkeletonData));
                prcDSThd.Start();
            }
        }

        //绘制相关数据初始化.
        private void InitializeDisplayImageData()
        {
            // Initialize depth map stuff
            usersLblTex = new Texture2D(ImiWrapper.DEPTHWIDTH, ImiWrapper.DEPTHHEIGHT, TextureFormat.RGBA32, false);
            usersDepthColors = new byte[depthMapSize * 4];

            // Initialize color map stuff
            userClrTex = new Texture2D(ImiWrapper.COLORWIDTH, ImiWrapper.COLORHEIGHT, TextureFormat.RGB24, false);
            colorData = new byte[ImiWrapper.COLORWIDTH * ImiWrapper.COLORHEIGHT * 3];

            usersHistogramMap = new float[65536];
        }

        //体感相关数据初始化.
        private void InitializeIminectData()
        {

            depthData = new ushort[depthMapSize];
            depthPlayerData = new ushort[depthMapSize];
            skeletonJointsCount = (int)ImiWrapper.ImiSkeletonPositionIndex.IMI_SKELETON_POSITION_COUNT;
            playerInfos = new Dictionary<uint, ImiPlayerInfo>();
        }

        //void OnApplicationQuit()
        //{
        //    Debug.Log("On ApplicationQuit");
        //    isPrcStop = true;
        //    isClosed = true;
        //    Close();
        //}


        /// <summary>
        /// Open Device and Open Streams according to the user choice
        /// </summary>
        private void IminectInitialize()
        {
            //Returns if Device is unavailable
            //Returns if Already Initialized
            if (!imiManager.IsDeviceAvailable() || iminectInitialized)
            {
                return;
            }

            ImiWrapper.ErrorCode error;
            if ((error = imiManager.OpenDevice()) != ImiWrapper.ErrorCode.OK)
            {
                Debug.LogError(error);
                return;
            }

            //Open Depth/Skeleton Stream
            error = openDepthAndSkeletonStream();

            if (error != ImiWrapper.ErrorCode.OK)
            {
                Debug.LogError(error);
                return;
            }

            //Open Color Stream
            if (imiManager.processColorData)
            {
                if ((error = ImiWrapper.OpenStream(ImiWrapper.ImiFrameType.IMI_COLOR_FRAME,
                    ImiWrapper.ImiImageResolution.IMI_IMAGE_RESOLUTION_640x480,
                    ImiWrapper.ImiPixelFormat.IMI_PIXEL_FORMAT_IMAGE_RGB24))
                != ImiWrapper.ErrorCode.OK)
                {
                    Debug.LogError(error);
                    return;
                }
            }

            Debug.Log("Initialize success!");
            iminectInitialized = true;
        }

        /// <summary>
        /// This method open depth stream and/or skeleton streams properly
        /// User can only select one in the four : depth, skeleton, depth And skeleton, user depth and skeleton
        /// </summary>
        private ImiWrapper.ErrorCode openDepthAndSkeletonStream()
        {
            ImiWrapper.ImiFrameType streamType;

            if (imiManager.processUserDepthAndSkeleton)
            {
                streamType = ImiWrapper.ImiFrameType.IMI_USER_INDEX_SKELETON_FRAME;
                imiManager.processDepth = false;
                imiManager.processSkeleton = false;
                imiManager.processDepthSkeleton = false;
            }
            else if (imiManager.processDepthSkeleton)
            {
                streamType = ImiWrapper.ImiFrameType.IMI_DEPTH_SKELETON_FRAME;
                imiManager.processDepth = false;
                imiManager.processSkeleton = false;
            }
            else if (imiManager.processDepth)
            {
                streamType = ImiWrapper.ImiFrameType.IMI_DEPTH_FRAME;
                imiManager.processSkeleton = false;
            }
            else if (imiManager.processSkeleton)
            {
                streamType = ImiWrapper.ImiFrameType.IMI_SKELETON_FRAME;
            }
            else
            {
                //Don't open any kind of depth stream or skeleton stream
                return ImiWrapper.ErrorCode.OK;
            }
            return ImiWrapper.OpenStream(streamType,
                ImiWrapper.ImiImageResolution.IMI_IMAGE_RESOLUTION_640x480,
                ImiWrapper.ImiPixelFormat.IMI_PIXEL_FORMAT_DEP_16BIT);

        }


        private void PrcDepthData()
        {
            while (!isPrcStop)
            {
                if (iminectInitialized)
                {
                    retDepth = ImiWrapper.GetDepthData(depthData, 30);

                    if (retDepth == ImiWrapper.ErrorCode.OK)
                    {
                        if (!prcDepthDrawLock)
                        {
                            UpdateDepthDrawData();
                            prcDepthDrawLock = true;
                        }
                        imiManager.Enqueue(UpdateDepthTex());

                    }
                }
                Thread.Sleep(THREAD_INTERVAL);
            }
        }

        private void PrcSkeletonData()
        {
            while (!isPrcStop)
            {
                if (iminectInitialized)
                {
                    retDepth = ImiWrapper.GetSkeletonFrame(ref skeletonFrame, 30);

                    if (retDepth == ImiWrapper.ErrorCode.OK)
                    {
                        ProcessSkeleton();
                    }
                }
                Thread.Sleep(THREAD_INTERVAL);
            }

        }

        private void PrcDepthAndSkeletonData()
        {
            while (!isPrcStop)
            {
                if (iminectInitialized)
                {
                    retDepth = ImiWrapper.GetDepthDataAndSkeletonFrame(depthData, ref skeletonFrame, 30);

                    if (retDepth == ImiWrapper.ErrorCode.OK)
                    {
                        ProcessSkeleton();

                        if (!prcDepthDrawLock)
                        {
                            UpdateDepthDrawData();
                            prcDepthDrawLock = true;
                        }
                        imiManager.Enqueue(UpdateDepthTex());

                    }
                }
                Thread.Sleep(THREAD_INTERVAL);
            }
        }

        //体感数据处理.
        private void PrcImiDepthAndSkeletonData()
        {
            while (!isPrcStop)
            {
                if (iminectInitialized)
                {
                    retDepth = ImiWrapper.GetDepthPlayerDataAndSkeletonFrame(depthPlayerData, ref skeletonFrame, 30);

                    if (retDepth == ImiWrapper.ErrorCode.OK)
                    {
                        ProcessSkeleton();

                        if (!prcDepthDrawLock)
                        {
                            UpdateUserDepthDrawData();
                            prcDepthDrawLock = true;
                        }
                        imiManager.Enqueue(UpdateDepthTex());

                    }
                }
                Thread.Sleep(THREAD_INTERVAL);
            }
        }

        private int skipFrame;
        private void PrcColorData()
        {
            while (!isPrcStop)
            {
                if (iminectInitialized)
                {
                    skipFrame++;
                    if (skipFrame % 1 == 0)
                    {
                        retColor = ImiWrapper.GetColorData(colorData, 30);
                        if (retColor == ImiWrapper.ErrorCode.OK)
                        {
                            imiManager.Enqueue(UpdateColorTex());
                        }

                    }

                }
                Thread.Sleep(THREAD_INTERVAL);
            }
        }


        //Remove users who entered before and now no longer been tracked
        private void removeUnTrackedUser()
        {
            List<uint> keys = new List<uint>();
            foreach (KeyValuePair<uint, ImiPlayerInfo> pair in playerInfos)
            {
                if (pair.Key != skeletonFrame.skeletonData[0].usrIndex &&
                   pair.Key != skeletonFrame.skeletonData[1].usrIndex)
                {
                    keys.Add(pair.Key);
                }
            }
            foreach (uint key in keys)
            {
                playerInfos.Remove(key);
            }

        }

        //骨架数据处理.
        private void ProcessSkeleton()
        {

            if (skeletonFrame.skeletonData == null)
            {
                return;
            }
            removeUnTrackedUser();

            //process every skeleton in the frame
            for (int i = 0; i < ImiWrapper.MAX_TRACKED_PEOPLE_NUM; i++)
            {
                ImiWrapper.ImiSkeletonData skeletonData = skeletonFrame.skeletonData[i];
                //uint userId = (uint)(i + 1);
                //process the tracked skeleton
                if (skeletonData.trackingState == ImiWrapper.ImiSkeletonTrackingState.IMI_SKELETON_TRACKED)
                {
                    uint userId = (uint)skeletonData.usrIndex;
                    //init playerInfo
                    ImiPlayerInfo playerInfo = null;
                    playerInfos.TryGetValue(userId, out playerInfo);
                    if (playerInfo == null)
                    {
                        playerInfo = new ImiPlayerInfo();
                        playerInfos.Add(userId, playerInfo);
                    }
                    playerInfo.SetPlayerCalibrated(true);
                    playerInfo.SetUserId(userId);

                    int stateNotTracked = (int)ImiWrapper.ImiSkeletonTrackingState.IMI_SKELETON_NOT_TRACKED;

                    playerInfo.SetPlayerPosition(new Vector3(skeletonData.position.x, skeletonData.position.y, skeletonData.position.z));
                    for (int j = 0; j < skeletonJointsCount; j++)
                    {
                        playerInfo.SetPlayerJointTracked(j, ((int)skeletonData.skeletonPositionTrackingStates[j] != stateNotTracked));
                        playerInfo.SetPlayerJointPos(j, new Vector3(skeletonData.skeletonPositions[j].x, skeletonData.skeletonPositions[j].y, skeletonData.skeletonPositions[j].z));
                        playerInfo.jointsPosV4[j] = skeletonData.skeletonPositions[j];
                    }
                }
            }
        }

        //前景图绘制数据处理.
        void UpdateUserDepthDrawData()
        {
            int numOfPoints = 0;

            Array.Clear(usersHistogramMap, 0, usersHistogramMap.Length);
            Array.Clear(usersDepthColors, 0, usersDepthColors.Length);

            for (int i = 0; i < depthMapSize; i++)
            {
                usersHistogramMap[depthPlayerData[i] >> 3]++;
                numOfPoints++;
            }

            if (numOfPoints > 0)
            {
                for (int i = 1; i < usersHistogramMap.Length; i++)
                {
                    usersHistogramMap[i] += usersHistogramMap[i - 1];
                }

                for (int i = 0; i < usersHistogramMap.Length; i++)
                {
                    usersHistogramMap[i] = 1.0f - (usersHistogramMap[i] / numOfPoints);
                }
            }

            for (int i = 0; i < depthMapSize; i++)
            {
                int tempPlayerIndex = depthPlayerData[i] & 7;
                int userDepth = depthPlayerData[i] >> 3;
                float histDepth = usersHistogramMap[userDepth];
                //float histDepth = 1;

                switch (tempPlayerIndex)
                {
                    case 1:
                        usersDepthColors[i * 4] = (byte)(255 * histDepth);
                        usersDepthColors[i * 4 + 1] = (byte)(105 * histDepth);
                        usersDepthColors[i * 4 + 2] = (byte)(180 * histDepth);
                        usersDepthColors[i * 4 + 3] = 255;
                        break;
                    case 2:
                        usersDepthColors[i * 4] = (byte)(255 * histDepth);
                        usersDepthColors[i * 4 + 1] = (byte)(0 * histDepth);
                        usersDepthColors[i * 4 + 2] = (byte)(255 * histDepth);
                        usersDepthColors[i * 4 + 3] = 255;
                        break;
                    case 3:
                        usersDepthColors[i * 4] = (byte)(255 * histDepth);
                        usersDepthColors[i * 4 + 1] = (byte)(105 * histDepth);
                        usersDepthColors[i * 4 + 2] = (byte)(180 * histDepth);
                        usersDepthColors[i * 4 + 3] = 255;
                        break;
                    case 4:
                        usersDepthColors[i * 4] = (byte)(148 * histDepth);
                        usersDepthColors[i * 4 + 1] = (byte)(0 * histDepth);
                        usersDepthColors[i * 4 + 2] = (byte)(211 * histDepth);
                        usersDepthColors[i * 4 + 3] = 255;
                        break;
                    case 5:
                        usersDepthColors[i * 4] = (byte)(0 * histDepth);
                        usersDepthColors[i * 4 + 1] = (byte)(0 * histDepth);
                        usersDepthColors[i * 4 + 2] = (byte)(255 * histDepth);
                        usersDepthColors[i * 4 + 3] = 255;
                        break;
                    case 6:
                        usersDepthColors[i * 4] = (byte)(255 * histDepth);
                        usersDepthColors[i * 4 + 1] = (byte)(215 * histDepth);
                        usersDepthColors[i * 4 + 2] = (byte)(0 * histDepth);
                        usersDepthColors[i * 4 + 3] = 255;
                        break;
                    default:
                        usersDepthColors[i * 4] = (byte)(125 * histDepth);
                        usersDepthColors[i * 4 + 1] = (byte)(125 * histDepth);
                        usersDepthColors[i * 4 + 2] = (byte)(125 * histDepth);
                        usersDepthColors[i * 4 + 3] = 255;
                        break;
                }
            }
        }

        private void UpdateDepthDrawData()
        {
            int numOfPoints = 0;

            Array.Clear(usersHistogramMap, 0, usersHistogramMap.Length);
            Array.Clear(usersDepthColors, 0, usersDepthColors.Length);

            for (int i = 0; i < depthMapSize; i++)
            {
                usersHistogramMap[depthData[i]]++;
                numOfPoints++;
            }

            if (numOfPoints > 0)
            {
                for (int i = 1; i < usersHistogramMap.Length; i++)
                {
                    usersHistogramMap[i] += usersHistogramMap[i - 1];
                }

                for (int i = 0; i < usersHistogramMap.Length; i++)
                {
                    usersHistogramMap[i] = 1.0f - (usersHistogramMap[i] / numOfPoints);
                }
            }

            for (int i = 0; i < depthMapSize; i++)
            {
                int userDepth = depthData[i];
                float histDepth = usersHistogramMap[userDepth];
                //float histDepth = 1;

                usersDepthColors[i * 4] = (byte)(125 * histDepth);
                usersDepthColors[i * 4 + 1] = (byte)(125 * histDepth);
                usersDepthColors[i * 4 + 2] = (byte)(125 * histDepth);
                usersDepthColors[i * 4 + 3] = 255;
            }
        }

        public IEnumerator UpdateColorTex()
        {
            userClrTex.LoadRawTextureData(colorData);
            userClrTex.Apply();
            yield return null;
        }

        private IEnumerator UpdateDepthTex()
        {
            if (prcDepthDrawLock)
            {
                usersLblTex.LoadRawTextureData(usersDepthColors);
                usersLblTex.Apply();

                prcDepthDrawLock = false;
            }
            yield return null;
        }


    }

}
