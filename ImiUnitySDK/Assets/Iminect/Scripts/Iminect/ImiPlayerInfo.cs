//***********************************************************************************************************************
//
//文件名(File Name):     IminectPlayerInfo .cs
//
//功能描述(Description):    玩家体感数据存储类
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
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using IMIForUnity;

namespace IMIForUnity
{

    public class ImiPlayerInfo
    {
        private uint userId;
        private bool[] playerJointsTracked;
        private bool playerCalibrated;
        private Vector3 playerPosition;
        private Vector3[] playerJointsPos;

        public ImiWrapper.ImiVector4[] jointsPosV4;

        public ImiPlayerInfo()
        {
            this.userId = 0;
            this.playerJointsTracked = new bool[(int)ImiWrapper.ImiSkeletonPositionIndex.IMI_SKELETON_POSITION_COUNT];
            this.playerCalibrated = false;
            this.playerPosition = new Vector3();
            this.playerJointsPos = new Vector3[(int)ImiWrapper.ImiSkeletonPositionIndex.IMI_SKELETON_POSITION_COUNT];

            jointsPosV4 = new ImiWrapper.ImiVector4[(int)ImiWrapper.ImiSkeletonPositionIndex.IMI_SKELETON_POSITION_COUNT];
        }

        public void SetUserId(uint id)
        {
            this.userId = id;
        }

        public void SetPlayerJointTracked(int joint, bool tracked)
        {
            this.playerJointsTracked[joint] = tracked;
        }

        public void SetPlayerCalibrated(bool calibrated)
        {
            this.playerCalibrated = calibrated;
        }

        public void SetPlayerPosition(Vector3 position)
        {
            this.playerPosition = position;
        }

        public void SetPlayerJointPos(int joint, Vector3 position)
        {
            this.playerJointsPos[joint] = position;
        }

        public uint GetUserId()
        {
            return this.userId;
        }

        public bool[] GetPlayerJointsTracked()
        {
            return this.playerJointsTracked;
        }


        public bool GetPlayerCalibrated()
        {
            return this.playerCalibrated;
        }

        public Vector3 GetPlayerPosition()
        {
            return this.playerPosition;
        }

        public Vector3[] GetPlayerJointsPos()
        {
            return this.playerJointsPos;
        }
    }

}
