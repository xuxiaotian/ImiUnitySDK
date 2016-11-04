using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using IMIForUnity;
using UnityEngine.UI;

public class ImiExplorer : MonoBehaviour
{

    //Parent of all Skeletons
    public GameObject skeletons;
    private List<GameObject> skeletonList = new List<GameObject>();

    //Displaying Color Image
    public RawImage colorView;

    //Displaying Depth Image
    public RawImage depthView;

    private int skeletonCount = (int)ImiWrapper.ImiSkeletonPositionIndex.IMI_SKELETON_POSITION_COUNT;


    public Text label;

    // Use this for initialization
    void Start()
    {

        Debug.Log("skeletonCount = " + skeletonCount);
        int skeletonsNum = ImiWrapper.MAX_TRACKED_PEOPLE_NUM * skeletonCount;
        for (int i = 0; i < skeletonsNum; i++)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.parent = skeletons.transform;
            sphere.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            skeletonList.Add(sphere);
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (ImiManager.GetInstance().IsInitialized())
        {
            colorView.texture = ImiManager.GetInstance().GetUserColorTexture();
            depthView.texture = ImiManager.GetInstance().GetDepthTexture();

            Dictionary<uint, ImiPlayerInfo> playerInfos = ImiManager.GetInstance().GetPlayerInfos();

            if (playerInfos.Count > 0)
            {
                int playerIndex = 0;
                //Debug.Log("Tracked Player Count = " + ImiManager.GetInstance().playerInfos.Count);
                foreach (KeyValuePair<uint, ImiPlayerInfo> pair in playerInfos)
                {

                    if(playerIndex >= ImiWrapper.MAX_TRACKED_PEOPLE_NUM)
                    {
                        break;
                    }
                    Vector3[] jointsPos = pair.Value.GetPlayerJointsPos();

                    for (int i = 0; i < skeletonCount; i++)
                    {
                        skeletonList[i + playerIndex * skeletonCount].transform.position = jointsPos[i];
                        //Debug.Log("i = " + i + jointsPos[i].ToString());
                    }
                    playerIndex++;

                    //testConversion(pair.Value.GetUserId());
                }

            }
        }

    }

    private void testConversion(uint controlId)
    {
        int depthX = 0;
        int depthY = 0;
        int depthZ = 0;
        Dictionary<uint, ImiPlayerInfo> playerInfos = ImiManager.GetInstance().GetPlayerInfos();

        ImiWrapper.ImiVector4 jointPosV4 = playerInfos[controlId].
            jointsPosV4[(int)ImiWrapper.ImiSkeletonPositionIndex.IMI_SKELETON_POSITION_HAND_RIGHT];

        label.text = jointPosV4.x.ToString() + " " + jointPosV4.y.ToString() + " " + jointPosV4.z.ToString();


        //ImiWrapper.ConvertSkeletonPointToDepthPoint(
        //    jointPosV4,
        //    ref depthX,
        //    ref depthY,
        //    ref depthZ,
        //    ImiWrapper.ImiImageResolution.IMI_IMAGE_RESOLUTION_640x480);

        //ImiWrapper.ImiVector4 _jointPosV4 = new ImiWrapper.ImiVector4();
        //ImiWrapper.ConvertDepthPointToSkeletonPoint(
        //    depthX,
        //    depthY,
        //    depthZ,
        //    ref _jointPosV4,
        //    ImiWrapper.ImiImageResolution.IMI_IMAGE_RESOLUTION_640x480);
        //label.text = _jointPosV4.x.ToString() + " " + _jointPosV4.y.ToString() + " " + _jointPosV4.z.ToString() + " " + depthX.ToString() + " " + depthY.ToString() + " " + depthZ.ToString();

    }

}
