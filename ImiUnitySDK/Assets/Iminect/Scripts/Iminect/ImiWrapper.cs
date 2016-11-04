using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.IO;
using System.Text;

namespace IMIForUnity
{
    public class ImiWrapper
    {
#if UNITY_ANDROID
        private const string filePath = "IminectUnity";
#else
        private const string filePath = "ImiUnitySDK";
#endif

        public const int MAX_TRACKED_PEOPLE_NUM = 2;

        public const int DEPTHWIDTH = 640;

        public const int DEPTHHEIGHT = 480;

        //public const int COLORWIDTH = 1280;
        public const int COLORWIDTH = 640;
        //public const int COLORWIDTH = 1920;

        //public const int COLORHEIGHT = 720;
        public const int COLORHEIGHT = 480;
        //public const int COLORHEIGHT = 1080;


        public enum ErrorCode
        {
            NONE = -1,
            OK,
            INITIALIZE_FAILED,
            DEVICE_NOT_FIND,
            OPEN_DEVICE_FAILED,
            SET_DEPTH_FRAME_MODE_FAILED,
            SET_SKELETON_FRAME_MODE_FAILED,
            SET_DEPTH_SKELETON_FRAME_MODE_FAILED,
            SET_USER_SKELETON_FRAME_MODE_FAILED,
            SET_COLOR_FRAME_MODE_FAILED,
            OPEN_DEPTH_STEAM_FAILED,
            OPEN_DEPTH_SKELETON_STEAM_FAILED,
            OPEN_SKELETON_STEAM_FAILED,
            OPEN_USER_SKELETON_STEAM_FAILED,
            OPEN_COLOR_STEAM_FAILED,
            DEPTH_STREAM_NOT_OPEN,
            SKELETON_STREAM_NOT_OPEN,
            DEPTH_SKELETON_STREAM_NOT_OPEN,
            USER_SKELETON_STREAM_NOT_OPEN,
            COLOR_STREAM_NOT_OPEN,
            CLOSE_DEPTH_STREAM_FAILED,
            CLOSE_SKELETON_STREAM_FAILED,
            CLOSE_DEPTH_SKELETON_STREAM_FAILED,
            CLOSE_USER_SKELETON_STREAM_FAILED,
            CLOSE_COLOR_STREAM_FAILED,
            DEPTH_FRAME_DATA_NULL,
            SKELETON_FRAME_DATA_NULL,
            DEPTH_SKELETON_FRAME_DATA_NULL,
            USER_SKELETON_FRAME_DATA_NULL,
            COLOR_FRAME_DATA_NULL,
            READ_DEPTH_FRAME_FAILED,
            READ_SKELETON_FRAME_FAILED,
            READ_DEPTH_SKELETON_FRAME_FAILED,
            READ_USER_SKELETON_FRAME_FAILED,
            READ_COLOR_FRAME_FAILED,
            CLOSE_DEVICE_FAILED,
            RELEASE_DEVICE_LIST_FAILED,
            DESTROY_DEVICE_FAILED
        }

        public enum ImiDeviceState
        {
            IMI_DEVICE_STATE_CONNECT 	= 0,
            IMI_DEVICE_STATE_DISCONNECT = 1
        }

        public struct ImiVector4
        {
            public float x;
            public float y;
            public float z;
            public float w;
        }

        public enum ImiImageResolution
        {
            IMI_IMAGE_RESOLUTION_INVALID = -1,
            IMI_IMAGE_RESOLUTION_320x240 = 0,
            IMI_IMAGE_RESOLUTION_640x480 = (IMI_IMAGE_RESOLUTION_320x240 + 1),
            IMI_IMAGE_RESOLUTION_1280x720 = (IMI_IMAGE_RESOLUTION_640x480 + 1),
            IMI_IMAGE_RESOLUTION_1920x1080 = (IMI_IMAGE_RESOLUTION_1280x720 + 1)
        }

        /// <summary>
        /// Types of frame provided by devices
        /// The device can only transfer two frames at most, one is color frame,
        /// the other is one kind in the four: depth, skeleton, depth and skeleton, user depth and skeleton
        /// </summary>
        public enum ImiFrameType
        {
            //Provide Depth Frame Only, Depth Frame is 640*480*2 B
            IMI_DEPTH_FRAME = 0x00,

            //Provide Skeleton Frame Only, Skeleton Frame is transported in struct
            IMI_SKELETON_FRAME = 0x03,

            //Provide Both Depth Frame and Skeleton Frame, which equals depth fram + skeleton frame
            IMI_DEPTH_SKELETON_FRAME = 0x01,//320*240 only

            //Provide Depth Frame with User Index And Skeleton Data
            //This frame is different from IMI_DEPTH_SKELETON_FRAME only that in this frame,
            //fir 3 bit of depth data is used for user index
            //This frame contains both depth frame and skeleton data
            IMI_USER_INDEX_SKELETON_FRAME = 0x02,//320*240 only

            //Provide Color Frame, Independent from the other four frames
            IMI_COLOR_FRAME = 0x04
        }

        public enum ImiPixelFormat
        {
            IMI_PIXEL_FORMAT_DEP_16BIT = 0x00, //Depth
            IMI_PIXEL_FORMAT_IMAGE_YUV422 = 0x01, //Rgb
            IMI_PIXEL_FORMAT_IMAGE_H264 = 0x02, //H264 Compressed
            IMI_PIXEL_FORMAT_IMAGE_RGB24 = 0x03,
            IMI_PIXEL_FORMAT_IMAGE_YUV420SP = 0x04, //NV21
        }

        public enum Limb
        {
            Left_Forearm = 0,
            Left_Forearm_Wrist,
            Left_Postbrachium,
            Right_Forearm,
            Right_Forearm_Wrist,
            Right_Postbrachium,
            Left_Calf,
            Left_Calf_Ankle,
            Left_Thigh,
            Right_Calf,
            Right_Calf_Ankle,
            Right_Thigh,
            Spine,
            Count
        }

        public enum ImiSkeletonPositionIndex
        {
            IMI_SKELETON_POSITION_HIP_CENTER = 0,
            IMI_SKELETON_POSITION_SPINE,				// = 1, //(IMI_SKELETON_POSITION_HIP_CENTER + 1),
            IMI_SKELETON_POSITION_SHOULDER_CENTER,		// = 2, //(IMI_SKELETON_POSITION_SPINE + 1),
            IMI_SKELETON_POSITION_HEAD,					// = 3, //(IMI_SKELETON_POSITION_SHOULDER_CENTER + 1),
            IMI_SKELETON_POSITION_SHOULDER_LEFT,		// = 4, //(IMI_SKELETON_POSITION_HEAD + 1),
            IMI_SKELETON_POSITION_ELBOW_LEFT,			// = 5, //(IMI_SKELETON_POSITION_SHOULDER_LEFT + 1),
            IMI_SKELETON_POSITION_WRIST_LEFT,			// = 6, //(IMI_SKELETON_POSITION_ELBOW_LEFT + 1),
            IMI_SKELETON_POSITION_HAND_LEFT,			// = 7, //(IMI_SKELETON_POSITION_WRIST_LEFT + 1),
            IMI_SKELETON_POSITION_SHOULDER_RIGHT,		// = 8, //(IMI_SKELETON_POSITION_HAND_LEFT + 1),
            IMI_SKELETON_POSITION_ELBOW_RIGHT,			// = 9, //(IMI_SKELETON_POSITION_SHOULDER_RIGHT + 1),
            IMI_SKELETON_POSITION_WRIST_RIGHT,			// = 10, //(IMI_SKELETON_POSITION_ELBOW_RIGHT + 1),
            IMI_SKELETON_POSITION_HAND_RIGHT,			// = 11, //(IMI_SKELETON_POSITION_WRIST_RIGHT + 1),
            IMI_SKELETON_POSITION_HIP_LEFT,				// = 12, //(IMI_SKELETON_POSITION_HAND_RIGHT + 1),
            IMI_SKELETON_POSITION_KNEE_LEFT,			// = 13, //(IMI_SKELETON_POSITION_HIP_LEFT + 1),
            IMI_SKELETON_POSITION_ANKLE_LEFT,			// = 14, //(IMI_SKELETON_POSITION_KNEE_LEFT + 1),
            IMI_SKELETON_POSITION_FOOT_LEFT,			// = 15, //(IMI_SKELETON_POSITION_ANKLE_LEFT + 1),
            IMI_SKELETON_POSITION_HIP_RIGHT,			// = 16, //(IMI_SKELETON_POSITION_FOOT_LEFT + 1),
            IMI_SKELETON_POSITION_KNEE_RIGHT,			// = 17, //(IMI_SKELETON_POSITION_HIP_RIGHT + 1),
            IMI_SKELETON_POSITION_ANKLE_RIGHT,			// = 18, //(IMI_SKELETON_POSITION_KNEE_RIGHT + 1),
            IMI_SKELETON_POSITION_FOOT_RIGHT,			// = 19, //(IMI_SKELETON_POSITION_ANKLE_RIGHT + 1),
            IMI_SKELETON_POSITION_COUNT					// = 20  //(IMI_SKELETON_POSITION_FOOT_RIGHT + 1)		// 20
        }

        public enum ImiSkeletonPositionTrackingState
        {
            IMI_SKELETON_POSITION_NOT_TRACKED = 0,
            IMI_SKELETON_POSITION_INFERRED,			// = (IMI_SKELETON_POSITION_NOT_TRACKED + 1) ,
            IMI_SKELETON_POSITION_TRACKED			// = (IMI_SKELETON_POSITION_INFERRED + 1)
        }

        public enum ImiSkeletonTrackingState
        {
            IMI_SKELETON_NOT_TRACKED = 0,
            IMI_SKELETON_POSITION_ONLY,		// = (IMI_SKELETON_NOT_TRACKED + 1),
            IMI_SKELETON_TRACKED			// = (IMI_SKELETON_POSITION_ONLY + 1)
        }

        public struct ImiSkeletonData
        {
            public ImiSkeletonTrackingState trackingState;
            public uint trackingID;
            public uint enrollIndex;
            public uint usrIndex;
            //Player Position
            public ImiVector4 position;

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 20, ArraySubType = UnmanagedType.Struct)]
            public ImiVector4[] skeletonPositions;

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 20, ArraySubType = UnmanagedType.Struct)]
            public ImiSkeletonPositionTrackingState[] skeletonPositionTrackingStates;

            public uint   qualityFlags;
        }

        public struct ImiSkeletonFrame
        {
            //Floor where users stand
            public ImiVector4 floorClipPlane;

            //Skeleton Data, maxium users 2
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2, ArraySubType = UnmanagedType.Struct)]
            public ImiSkeletonData[] skeletonData;
        }

        public struct ImiSkeletonJointOrientation
        {
            public ImiSkeletonPositionIndex endJoint;
            public ImiSkeletonPositionIndex startJoint;
            public IMI_SKELETON_BONE_ROTATION hierarchicalRotation;
            public IMI_SKELETON_BONE_ROTATION absoluteRotation;
        }

        public struct IMI_SKELETON_BONE_ROTATION
        {
            public ImiMatrix4 rotationMatrix;
            public ImiVector4 rotationQuaternion;
        }

        public struct ImiMatrix4
        {
            public float M11;
            public float M12;
            public float M13;
            public float M14;
            public float M21;
            public float M22;
            public float M23;
            public float M24;
            public float M31;
            public float M32;
            public float M33;
            public float M34;
            public float M41;
            public float M42;
            public float M43;
            public float M44;
        }

        /// <summary>
        /// 打开设备并初始化,适用于所有平台，以及root过了的Android平台
        /// </summary>
        /// <returns>
        /// 0：成功.
        /// 1：设备初始化失败.
        /// 2：设备未找到.
        /// 3：打开失败.
        /// </returns>
        [DllImport(filePath, EntryPoint = "OpenDevice")]
        public static extern ErrorCode OpenDevice();


        /// <summary>
        /// 功能同OpenDevice，专门为Android平台所提供的接口，在获取读写Usb权限后，根据Android返回的fd打开设备
        /// </summary>
        /// <param name="fd">File Descriptor</param>
        /// <param name="path">Usb Device Absolute Path</param>
        /// <returns>同OpenDevice()</returns>
        [DllImport(filePath, EntryPoint = "OpenDevice2")]
        public static extern ErrorCode OpenDevice2(int fd, string path);

        /// <summary>
        /// 关闭设备.
        /// </summary>
        [DllImport(filePath, EntryPoint = "CloseDevice")]
        public static extern ErrorCode CloseDevice();

        /// <summary>
        /// 打开数据流.
        /// </summary>
        /// <param name="frameType">数据帧枚举类型.</param>
        /// <param name="imageResolution"></param>
        /// <param name="pixelFormat"></param>
        /// <returns></returns>
        [DllImport(filePath, EntryPoint = "OpenStream")]
        public static extern ErrorCode OpenStream(ImiFrameType frameType, ImiImageResolution imageResolution, ImiPixelFormat pixelFormat);

        [DllImport(filePath, EntryPoint = "CloseStream")]
        public static extern ErrorCode CloseStream(ImiFrameType frameType);

        [DllImport(filePath, EntryPoint = "GetDepthData")]
        public static extern ErrorCode GetDepthData(ushort[] depthData, uint timeOut);

        [DllImport(filePath, EntryPoint = "GetDepthDataAndSkeletonFrame")]
        public static extern ErrorCode GetDepthDataAndSkeletonFrame(ushort[] depthData, ref ImiSkeletonFrame skeletonFrame, uint timeOut);

        [DllImport(filePath, EntryPoint = "GetSkeletonFrame")]
        public static extern ErrorCode GetSkeletonFrame(ref ImiSkeletonFrame skeletonFrame, uint timeOut);

        [DllImport(filePath, EntryPoint = "GetDepthPlayerDataAndSkeletonFrame")]
        public static extern ErrorCode GetDepthPlayerDataAndSkeletonFrame(ushort[] depthPlayerData, ref ImiSkeletonFrame skeletonFrame, uint timeOut);

        /// <summary>
        /// 获取彩色数据.
        /// </summary>
        /// <param name="colorData">彩色数据（kinect存储类型为BGRA）.</param>
        /// <param name="timeOut">等待时间.</param>
        /// <returns>
        /// 0：成功.
        /// 1：失败.
        /// </returns>
        [DllImport(filePath, EntryPoint = "GetColorData")]
        public static extern ErrorCode GetColorData(byte[] colorData, uint timeOut);

        /// <summary>
        /// 是否开启骨架平滑.
        /// </summary>
        /// <param name="isEnable">true:开启 false:关闭</param>
        [DllImport(filePath, EntryPoint = "EnableSmoothSkeletonFrame")]
        public static extern void EnableSmoothSkeletonFrame(bool isEnable);

        /// <summary>
        /// 骨架坐标转换成深度坐标
        /// </summary>
        /// <param name="skeletonPosition">骨架坐标</param>
        /// <param name="depthX">深度X坐标</param>
        /// <param name="depthY">深度Y坐标</param>
        /// <param name="depthZ">深度值</param>
        /// <param name="depthReslution">深度图分辨率</param>
        /// <returns>0表示成功，1表示失败</returns>
        [DllImport(filePath, EntryPoint = "ConvertSkeletonPointToDepthPoint")]
        public static extern void ConvertSkeletonPointToDepthPoint(ImiVector4 skeletonPosition, ref int depthX, ref int depthY, ref int depthZ, ImiImageResolution depthReslution);

        /// <summary>
        /// 深度坐标转换成彩色坐标
        /// </summary>
        /// <param name="depthX">深度X坐标</param>
        /// <param name="depthY">深度Y坐标</param>
        /// <param name="depthZ">深度值</param>
        /// <param name="colorX">彩色X坐标</param>
        /// <param name="colorY">彩色Y坐标</param>
        /// <param name="depthReslution">深度图分辨率</param>
        /// <param name="colorReslution">彩色图分辨率</param>
        /// <returns>0表示成功，1表示失败</returns>
        [DllImport(filePath, EntryPoint = "ConvertDepthPointToColorPoint")]
        public static extern void ConvertDepthPointToColorPoint(int depthX, int depthY, int depthZ, ref int colorX, ref int colorY, ImiImageResolution depthReslution, ImiImageResolution colorReslution);

        /// <summary>
        /// 深度坐标转换成骨架坐标
        /// </summary>
        /// <param name="depthX">深度X坐标</param>
        /// <param name="depthY">深度Y坐标</param>
        /// <param name="depthZ">深度值</param>
        /// <param name="skeletonPosition">骨架坐标</param>
        /// <param name="depthReslution">深度图分辨率</param>
        /// <returns>0表示成功，1表示失败</returns>
        [DllImport(filePath, EntryPoint = "ConvertDepthPointToSkeletonPoint")]
        public static extern void ConvertDepthPointToSkeletonPoint(int depthX, int depthY, int depthZ, ref ImiVector4 skeletonPosition, ImiImageResolution depthReslution);

        [DllImport(filePath, EntryPoint = "GetDeviceState")]
        public static extern ImiDeviceState GetDeviceState();
    }
}
