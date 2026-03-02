using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CursorManager : MonoBehaviour {
    public Texture2D CursorMenu;
    public Texture2D CursorCrosshair;
    private void OnEnable() {
        Debug.Log("GameSetupManager Enable!!");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if(scene.name == ResourceSystem.SceneMenuName) {
            SetCursor(CursorType.Menu);
        }
    }

    private void OnDisable() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    public void SetCursor(CursorType type) {
        switch (type) {
            case CursorType.Menu:
                Cursor.SetCursor(Resources.Load<Texture2D>("cursorMenu"), new Vector2(3, 3), CursorMode.Auto);
                break;
            case CursorType.Crosshair:
                Cursor.SetCursor(Resources.Load<Texture2D>("cursorCrossHair"), new Vector2(10.5f, 10.5f), CursorMode.Auto);
                break;
            default:
                break;
        }
    }
}
public enum CursorType {
    Menu,
    Crosshair
}