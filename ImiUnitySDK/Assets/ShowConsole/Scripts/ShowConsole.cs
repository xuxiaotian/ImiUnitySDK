using UnityEngine;
using System.Collections;
using System.Collections.Generic;
/// <summary>
/// 文件名(File Name): ShowConsole.cs
/// 作者(Author): xw
/// 日期(Create Data): 2016.
/// </summary>
public class ShowConsole :MonoSingleton<ShowConsole> {

    public ResourceReferences references;

    void Awake()
    {
        ConsoleDisplay.Instance();
        ConsoleDisplay.Instance().RegisterLogEvent();
        DontDestroyOnLoad(gameObject);
    }

    public delegate void SDLCCallBack();
  
    public SDLCCallBack onUpdate;
    public SDLCCallBack onGUI;

    void Update()
    {
        if (onUpdate != null)
        {
            onUpdate();
        }
    }

    void OnGUI()
    {
        if (onGUI != null)
        {
            onGUI();
        }
    }
}

