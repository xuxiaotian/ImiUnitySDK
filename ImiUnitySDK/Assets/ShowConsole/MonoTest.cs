using UnityEngine;
using System.Collections;

public class MonoTest : MonoSingleton<MonoTest>
{
    public int t = 100;

    // Use this for initialization
    void Start()
    {        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log(MonoTest.Instance().t.ToString());
    }
}
