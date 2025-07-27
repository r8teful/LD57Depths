using UnityEngine;

// Root of all player UI
public class UIManager : Singleton<UIManager> {
    private InventoryManager _localInventoryManager;
    public PopupManager PopupManager { get; private set; }
    [field:SerializeField] public UIUpgradeScreen UpgradeScreen { get; private set; }
    [field: SerializeField] public UIManagerInventory UIManagerInventory {  get; private set; }
    private GameObject _playerGameObject;
    private NetworkedPlayerInventory _playerInventory;

    void Update() {
        UIManagerInventory.UpdateHeldItemVisual();
    }
    public void Init(InventoryManager localPlayerInvManager, GameObject owningPlayer, UpgradeManagerPlayer upgradeManager) {
        _localInventoryManager = localPlayerInvManager;
        PopupManager = GetComponent<PopupManager>();
        _playerGameObject = owningPlayer; // Important for knowing who to pass to item usage
        _playerInventory = _playerGameObject.GetComponent<NetworkedPlayerInventory>();

        if (_localInventoryManager == null || _playerGameObject == null) {
            Debug.LogError("InventoryUIManager received null references during Initialize! UI may not function.", gameObject);
            enabled = false;
            return;
        }


        // Init managers
        UpgradeScreen.Init(this,upgradeManager);
        UIManagerInventory.Init(this, localPlayerInvManager, owningPlayer);
        PopupManager.Init(localPlayerInvManager);
    }
}