using UnityEngine;

public class UIMenuManager : StaticInstance<UIMenuManager> {
    public bool IsPaused { get; set; }
    public Transform CanvasMain;
    private GameObject _currentPauseScreen;
    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            if (!IsPaused) {
                // Pause
                // Check if any shop menus are open
                if (ShipManager.Instance.IsAnyMenuOpen()) return;
                IsPaused = true;
                _currentPauseScreen = Instantiate(Resources.Load<GameObject>("UI/PauseScreen"), CanvasMain);
                Time.timeScale = 0;
            } else {
                // Resume
                IsPaused = false;
                Destroy(_currentPauseScreen);
                Time.timeScale = 1;
            }
        }
    }
    public void OnPauseClose() {
        // Resume
        IsPaused = false;
        Destroy(_currentPauseScreen);
        Time.timeScale = 1;
    }
}