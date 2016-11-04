using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public abstract class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
{
    protected static T instance = null;

    public static T Instance()
    {
        if (instance == null)
        {
            UnityEngine.Object[] instances = FindObjectsOfType<T>();
            int length = instances.Length;
            string name = typeof(T).Name;

            if (length == 1)
            {
                instance = (T)instances[0];
            }
            else if (length == 0)
            {                
                Debug.LogError("There is no " + name + "script on a GameObject in your scene.");
            }
            else if (length > 1)
            {
                Debug.LogError("There needs to be only one active " + name + "script on a GameObject in your scene.But there are " + length + " exist in game.");
            }
        }

        return instance;
    }


    protected virtual void OnDestroy()
    {
        instance = null;
    }
}
