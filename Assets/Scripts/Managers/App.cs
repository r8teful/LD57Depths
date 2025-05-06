using System;
using UnityEngine;
using Object = UnityEngine.Object;
public static class App {
    public static BackdropManager Backdrop;
    //public static SaveManager SaveManager;
   // public static GameManager GameManager;
    public static AudioController AudioController;
    public static ResourceSystem ResourceSystem;
    //public static EventManager EventManager;
    public static bool isEditor;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Boostrap() {
        ResourceSystem = new ResourceSystem();
        ResourceSystem.AssembleResources();

        var app = Object.Instantiate(Resources.Load("App")) as GameObject; 
        if (app == null) {
            throw new ApplicationException();
        }
        Debug.Log("Initialised persistent managers");
        Object.DontDestroyOnLoad(app);
        //EventManager  = app.GetComponentInChildren<EventManager>();
        //GameManager = app.GetComponentInChildren<GameManager>();
        AudioController = app.GetComponentInChildren<AudioController>();
        //SaveManager = app.GetComponentInChildren<SaveManager>();
        Backdrop = app.GetComponentInChildren<BackdropManager>();

        Application.quitting += Shutdown;
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