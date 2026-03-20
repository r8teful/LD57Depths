using System;
using UnityEngine;

// Root of all player UI
public class UIManager : Singleton<UIManager> {
    private InventoryManager _inventory;
    public PopupManager PopupManager { get; private set; }
    [field:SerializeField] public UIUpgradeScreen UpgradeScreen { get; private set; }
    [field: SerializeField] public UIManagerInventory UIManagerInventory {  get; private set; }
    [field: SerializeField] public UIPlayerHUD PlayerHUD {  get; private set; }
    [field: SerializeField] public UISubControlPanel UISubControlPanel {  get; private set; }
    [field: SerializeField] public UISubInventory UISubInventory {  get; private set; }
    [field: SerializeField] public UIPauseScreen UIPause {  get; private set; }
    [field: SerializeField] public GameObject DebugStatMenu {  get; private set; }

    private GameObject _playerGameObject;
    private bool _isPaused;

    public static event Action<bool> OnUIOpenChange;

    public bool IsAnyUIOpen() {
        if (UpgradeScreen.IsOpen)
            return true;
        if (UISubControlPanel.IsOpen)
            return true;
        if (UIPause.IsOpen)
            return true;
        return false;
    }
    public MonoBehaviour GetOpenUIScript() {
        if (UpgradeScreen.IsOpen == true) return UpgradeScreen;
        if (UISubControlPanel.IsOpen == true) return UISubControlPanel;
        if (UIPause.IsOpen == true) return UIPause;
        return null;
    }
    public void TryOpenCloseUpgradeUI(bool setActive, out bool didSucceed) {
        if (UpgradeScreen.IsOpen == setActive) {
            didSucceed = false;
            return;
        }
        UpgradeScreen.PanelClose();
        didSucceed = true;
    }

    public void DebugStatsShow() {
        DebugStatMenu.SetActive(true);
    }
    public void DebugStatsHide() {
        DebugStatMenu.SetActive(false);
    }
    public void Init(PlayerManager client, GameObject owningPlayer) {
        _inventory = SubmarineManager.Instance.SubInventory;
        PopupManager = GetComponent<PopupManager>();
        _playerGameObject = owningPlayer; // Important for knowing who to pass to item usage
        if (_inventory == null || _playerGameObject == null) {
            Debug.LogError("InventoryUIManager received null references during Initialize! UI may not function.", gameObject);
            enabled = false;
            return;
        }


        // Init managers
        UpgradeScreen.Init(this,client);
        UIManagerInventory.Init(owningPlayer,client);
        PopupManager.Init(_inventory);
        PlayerHUD.Init(client);;
        UISubInventory.Init();
    }

    internal void ShowMessage(string v) {
    }

    internal void ControlPanelUIOpen() {
        UISubControlPanel.ControlPanelShow();
        OnUIOpenChange?.Invoke(true);
    }

    internal void ControlPanelUIClose() {
        UISubControlPanel.ControlPanelHide(); 
        OnUIOpenChange?.Invoke(false);
    }

    internal void ControlPanelUIToggle() {
        if (UISubControlPanel.IsOpen) {
            UISubControlPanel.ControlPanelHide();
            OnUIOpenChange?.Invoke(false);
        }else {
            UISubControlPanel.ControlPanelShow();
            OnUIOpenChange?.Invoke(true);
        }
    }

    internal void UpgradePanelUIToggle() {
        if (UpgradeScreen.IsOpen) {
            UpgradeScreen.PanelClose();
            OnUIOpenChange?.Invoke(false);
        }else {
            UpgradeScreen.PanelOpen();
            OnUIOpenChange?.Invoke(true);
        }
    }

    internal void UpgradePanelUIClose() {
        UpgradeScreen.PanelClose();
        OnUIOpenChange?.Invoke(false);
    }

    public void Pause() {
        if (_isPaused) return;
        PauseInternal();
    }

    private void PauseInternal() {
        Time.timeScale = 0f;
        
        //AudioListener.pause = true; 
        //Cursor.lockState = CursorLockMode.None;
        //Cursor.visible = true;

        // Dissable action maps?
        UIPause.OnPauseOpen();
        OnUIOpenChange?.Invoke(true);
        _isPaused = true;
    }

    public void Unpause() {
        Time.timeScale = 1f;
        //AudioListener.pause = false;

        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;

        UIPause.OnPauseClose();
        OnUIOpenChange?.Invoke(false);
        _isPaused = false;
    }
    public void DEBUGToggleALLUI() {
        var cg = GetComponent<CanvasGroup>();
        if(cg.alpha >= 1) {
            cg.alpha = 0.0f;
        } else {
            cg.alpha = 1.0f;
        }

    }

    internal bool TryCloseAnyOpenUI() {
        var open = GetOpenUIScript();
        if (open != null) { 
            if(open is UIUpgradeScreen) {
                UpgradePanelUIToggle();
                OnUIOpenChange?.Invoke(false);
                return true;
            }
            if (open is UISubInventory) {
                ControlPanelUIClose();
                OnUIOpenChange?.Invoke(false);
                return true;
            }
            if (open is UIPauseScreen) {
                OnUIOpenChange?.Invoke(false);
                Unpause();
                return true;
            }
        }
        return false;
    }

}