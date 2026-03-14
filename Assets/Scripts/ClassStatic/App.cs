using System;
using UnityEngine;
using Object = UnityEngine.Object;
public static class App {
    public static BackdropManager Backdrop;
    //public static SaveManager SaveManager;
   // public static GameManager GameManager;
    public static AudioController AudioController;
    public static ResourceSystem ResourceSystem;
    public static CursorManager CursorManager;
    public static GameManager GameManager;
    //public static EventManager EventManager;
    public static bool isEditor;
    public static bool isDebugMode;
    public static bool saveDataExists;
    public static bool SaveRunDataExists;
    public static bool isDemo;

    public static bool SteamConnection { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boostrap() {
        ResourceSystem = new ResourceSystem();
        ResourceSystem.AssembleResources();

        var app = Object.Instantiate(Resources.Load("App")) as GameObject; 
        if (app == null) {
            throw new ApplicationException();
        }
        Application.targetFrameRate = 60;
        Debug.Log("Initialised persistent managers");
        Object.DontDestroyOnLoad(app);
        //EventManager  = app.GetComponentInChildren<EventManager>();
        GameManager = app.GetComponentInChildren<GameManager>();
        AudioController = app.GetComponentInChildren<AudioController>();
        //SaveManager = app.GetComponentInChildren<SaveManager>();
        Backdrop = app.GetComponentInChildren<BackdropManager>();
        CursorManager = app.GetComponentInChildren<CursorManager>();
        Cursor.SetCursor(Resources.Load<Texture2D>("cursorMenu"), new Vector2(3, 3), CursorMode.Auto);
        
        if (SaveManager.TryLoad(out var data)) {
            saveDataExists = true;
            SaveRunDataExists = data.HasRunData;
            isDemo = data.buildType == "demo" || GameManager.DebugPlayDemo;
        }

#if UNITY_STANDALONE
        try {
            //SteamClient.Init(3639640);
            SteamConnection = true;
        } catch (Exception e) {
            Debug.Log(e);
            SteamConnection = false;
            // Something went wrong - it's one of these:
            //
            //     Steam is closed?
            //     Can't find steam_api dll?
            //     Don't have permission to play app?
            //
        }
#endif

        Application.quitting += Shutdown;
#if UNITY_EDITOR
        isEditor = true;
#endif
    }
    private static void Shutdown() {
        Debug.Log("App Shutdown sequence started.");
        //GameManager.CurrentRun.OnShutdown();

        // TOdo possible the following: 
        //SaveManager = null;
        //AudioController = null;
        //GameManager = null;
        //EventManager = null;
        //Backdrop = null;
        //ResourceSystem = null;
    }
}