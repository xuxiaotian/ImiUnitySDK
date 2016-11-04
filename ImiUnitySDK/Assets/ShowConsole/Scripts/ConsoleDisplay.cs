using UnityEngine;
using System.Collections;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 控制台GUI输出类
/// 包括FPS，内存使用情况，日志GUI输出
/// </summary>
public class ConsoleDisplay : Singleton<ConsoleDisplay>
{
   public struct ConsoleMessage
    {
        public readonly string message;
        public readonly string stackTrace;
        public readonly LogType type;

        public ConsoleMessage(string message, string stackTrace, LogType type)
        {
            this.message = message;
            this.stackTrace = stackTrace;
            this.type = type;
        }
    }


    public uint currentPage = 0;
    public uint numMax = 100;
    public int itsNumberOfPages = 1;

    bool showLogWindow = true;
         
    /// <summary>
    /// Update回调
    /// </summary>
    public delegate void OnUpdateCallback();
    /// <summary>
    /// OnGUI回调
    /// </summary>
    public delegate void OnGUICallback();

    public OnUpdateCallback onUpdateCallback = null;
    public OnGUICallback onGUICallback = null;
    /// <summary>
    /// FPS计数器
    /// </summary>
    private FPSCounter fpsCounter = null;
    /// <summary>
    /// 内存监视器
    /// </summary>
    private MemoryDetector memoryDetector = null;
    private bool showGUI = true;
    List<ConsoleMessage> entries = new List<ConsoleMessage>();
    Vector2 scrollPos;
    bool scrollToBottom = false;
    bool collapse;
    bool mTouching = false;
    
    bool showError = true;
    bool showWarning = true;
    bool showLog = true;
    bool showDetail = true;

    const int margin = 20;
    Rect windowRect = new Rect(margin + Screen.width * 0.5f, margin, Screen.width * 0.5f - (2 * margin), Screen.height - (2 * margin));

    GUIContent clearLabel = new GUIContent("Clear", "Clear the contents of the console.");
    GUIContent errorLabel = new GUIContent("Error", "Hide Error messages.");
    GUIContent warningLabel = new GUIContent("Warning", "Hide Warning messages.");
    GUIContent logLabel = new GUIContent("Log", "Hide Log messages.");
    GUIContent collapseLabel = new GUIContent("Collapse", "Hide repeated messages.");
    GUIContent scrollToBottomLabel = new GUIContent("ScrollToBottom", "Scroll bar always at bottom");
    GUIContent detailLabel = new GUIContent("Details", "Show details");

    GUIContent pageLabel = new GUIContent();

    string PageTip()
    {
        itsNumberOfPages = (int)Math.Ceiling((float)entries.Count / (float)numMax);
        string aString = string.Format("page {0}/{1}", currentPage + 1, Math.Max(itsNumberOfPages, 1));
        return aString;
    }

    public void StartCheck()
    {
         
    }

    private ConsoleDisplay()
    {
        this.fpsCounter = new FPSCounter(this);
        this.memoryDetector = new MemoryDetector(this);
        //this.showGUI = App.Instance().showLogOnGUI;
        
        ShowConsole.Instance().onUpdate += Update;
        ShowConsole.Instance().onGUI += OnGUI;
        //Application.logMessageReceived += HandleLog;
     
    }

    ~ConsoleDisplay()
    {
    
       // Application.logMessageReceived -= HandleLog;
    }

    public void RegisterLogEvent()
    {
        Application.RegisterLogCallback(HandleLog);
    }

    void Update()
    {

      

#if UNITY_EDITOR
        if (Input.GetKeyUp(KeyCode.F1))
            this.showGUI = !this.showGUI;
#elif UNITY_ANDROID
            if (Input.GetKeyUp(KeyCode.Escape))
                this.showGUI = !this.showGUI;
#elif UNITY_IOS
            if (!mTouching && Input.touchCount == 4)
            {
                mTouching = true;
                this.showGUI = !this.showGUI;
            } else if (Input.touchCount == 0){
                mTouching = false;
            }
#endif

        if (this.onUpdateCallback != null)
            this.onUpdateCallback();
    }

    void OnGUI()
    {
        if (!this.showGUI)
            return;

        if (this.onGUICallback != null)
            this.onGUICallback();

        if (GUI.Button(new Rect(20, 20, 60, 30), ShowConsole.Instance().references.itsOnOff,GUIStyle.none))
        {
            if (showLogWindow)
            {
                showLogWindow = false;
            }
            else
            {
                showLogWindow = true;
            }
            //PlayerPrefs.DeleteAll();
            //#if UNITY_EDITOR
            //            EditorApplication.isPlaying = false;
            //#else
            //            Application.Quit();
            //#endif
        }
        if (showLogWindow)
            windowRect = GUILayout.Window(123456, windowRect, ConsoleWindow, "Console");
    }


    /// <summary>
    /// A window displaying the logged messages.
    /// </summary>
    void ConsoleWindow(int windowID)
    {
        if (scrollToBottom)
        {
            GUILayout.BeginScrollView(Vector2.up * entries.Count * 100.0f);
        }
        else
        {
            scrollPos = GUILayout.BeginScrollView(scrollPos);
        }
        // Go through each logged entry
        int curMax = ((currentPage + 1) * numMax) < entries.Count ? (int)((currentPage + 1) * numMax) : entries.Count;
        for (int i = (int)(currentPage*numMax); i < curMax; i++)
        {
            ConsoleMessage entry = entries[i];
            // If this message is the same as the last one and the collapse feature is chosen, skip it
            if (collapse && i > 0 && entry.message == entries[i - 1].message)
            {
                continue;
            }
            if (!showError && entry.type == LogType.Error)
            {
                continue;
            }
            if (!showWarning && entry.type == LogType.Warning)
            {
                continue;
            }
            if (!showLog && entry.type == LogType.Log)
            {
                continue;
            }


            // Change the text colour according to the log type
            switch (entry.type)
            {
                case LogType.Error:
                case LogType.Exception:
                    GUI.contentColor = Color.red;
                    break;
                case LogType.Warning:
                    GUI.contentColor = Color.yellow;
                    break;
                default:
                    GUI.contentColor = Color.white;
                    break;
            }
            if (entry.type == LogType.Exception)
            {
                GUILayout.Label(entry.message + "  || " + entry.stackTrace);
            }
            else if (showDetail)
            {
                GUILayout.Label(entry.message + "     ||     " + entry.stackTrace);
            }
            else
            {
                GUILayout.Label(entry.message);
            }
        }
        GUI.contentColor = Color.white;
        GUILayout.EndScrollView();
        GUILayout.BeginHorizontal();
        // Clear button
        if (GUILayout.Button(clearLabel))
        {
            entries.Clear();
            currentPage = 0;
         
        }

        showError = GUILayout.Toggle(showError, errorLabel, GUILayout.ExpandWidth(false));
        showWarning = GUILayout.Toggle(showWarning, warningLabel, GUILayout.ExpandWidth(false));
        showLog = GUILayout.Toggle(showLog, logLabel, GUILayout.ExpandWidth(false));

        // Collapse toggle
        collapse = GUILayout.Toggle(collapse, collapseLabel, GUILayout.ExpandWidth(false));
        scrollToBottom = GUILayout.Toggle(scrollToBottom, scrollToBottomLabel, GUILayout.ExpandWidth(false));

        showDetail = GUILayout.Toggle(showDetail, detailLabel, GUILayout.ExpandWidth(true));

        if (GUILayout.Button(ShowConsole.Instance().references.itsIconLeft) && currentPage>0)
        {
            currentPage--;
        }
        GUILayout.Box(PageTip());

        if (GUILayout.Button(ShowConsole.Instance().references.itsIconRight) && currentPage<itsNumberOfPages-1)
        {
            currentPage++;
        }

        GUILayout.EndHorizontal();
        // Set the window to be draggable by the top title bar
        GUI.DragWindow(new Rect(0, 0, 10000, 20));

        //GUILayout.Button(
        //GUILayout.Toggle
    }

    void HandleLog(string message, string stackTrace, LogType type)
    {
        ConsoleMessage entry = new ConsoleMessage(message, stackTrace, type);
        entries.Add(entry);
    }

    
}
