using UnityEngine;

public class UIMenuManager : PersistentSingleton<UIMenuManager> {
    public bool IsPaused { get; set; }
    private void Update() {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            // Check if any shop menus are open
            if (ShipManager.Instance.IsAnyMenuOpen()) return;
            // TODO Pause menu
        }
    }
}