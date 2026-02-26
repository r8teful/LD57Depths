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
    public bool IsAnyUIOpen() {
        if (UIManagerInventory.IsOpen)
            return true;
        if (UpgradeScreen.IsOpen)
            return true;
        if (UISubControlPanel.IsOpen)
            return true;
        if (UIPause.IsOpen)
            return true;
        return false;
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

    public void DEBUGToggleALLUI() {
        var cg = GetComponent<CanvasGroup>();
        if(cg.alpha >= 1) {
            cg.alpha = 0.0f;
        } else {
            cg.alpha = 1.0f;
        }

    }

}