using UnityEngine;

// Root of all player UI
public class UIManager : Singleton<UIManager> {
    private InventoryManager _inventory;
    public PopupManager PopupManager { get; private set; }
    [field:SerializeField] public UIUpgradeScreen UpgradeScreen { get; private set; }
    [field: SerializeField] public UIManagerInventory UIManagerInventory {  get; private set; }
    [field: SerializeField] public UIManagerStats UIManagerStats {  get; private set; }
    [field: SerializeField] public UISubControlPanel UISubControlPanel {  get; private set; }
    [field: SerializeField] public UISubInventory UISubInventory {  get; private set; }
    [field: SerializeField] public UIPauseScreen UIPause {  get; private set; }

    private GameObject _playerGameObject;
    private bool _isPaused;

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
        UpgradeScreen.PanelToggle();
        didSucceed = true;
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
        UIManagerStats.Init(client);;
        UISubInventory.Init();
    }

    internal void ShowMessage(string v) {
    }

    internal void ControlPanelUIOpen() {
        UISubControlPanel.ControlPanelShow();
    }

    internal void ControlPanelUIClose() {
        UISubControlPanel.ControlPanelHide();
    }

    internal void ControlPanelUIToggle() {
        UISubControlPanel.ControlPanelToggle();
    }

    internal void UpgradePanelUIToggle() {
        UpgradeScreen.PanelToggle();
    }

    internal void UpgradePanelUIClose() {

        UpgradeScreen.PanelHide();
    }

    public void PausePanelUIToggle() {
        if (_isPaused) Unpause();
        else Pause();
    }

    private void Pause() {
        Time.timeScale = 0f;
        
        AudioListener.pause = true; 
        //Cursor.lockState = CursorLockMode.None;
        //Cursor.visible = true;

        // Dissable action maps?
        UIPause.OnPauseOpen();
        _isPaused = true;
    }

    private void Unpause() {
        Time.timeScale = 1f;
        AudioListener.pause = false;

        //Cursor.lockState = CursorLockMode.Locked;
        //Cursor.visible = false;

        UIPause.OnPauseClose();
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
                return true;
            }
            if (open is UISubInventory) {
                ControlPanelUIClose();
                return true;
            }
            if (open is UIPauseScreen) {
                PausePanelUIToggle();
                return true;
            }
        }
        return false;
    }

}