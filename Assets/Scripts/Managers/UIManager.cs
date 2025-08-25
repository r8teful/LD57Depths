using UnityEngine;

// Root of all player UI
public class UIManager : Singleton<UIManager> {
    private InventoryManager _localInventoryManager;
    public PopupManager PopupManager { get; private set; }
    [field:SerializeField] public UIUpgradeScreen UpgradeScreen { get; private set; }
    [field: SerializeField] public UIManagerInventory UIManagerInventory {  get; private set; }
    private GameObject _playerGameObject;

    public void Init(NetworkedPlayer client, GameObject owningPlayer) {
        _localInventoryManager = client.InventoryN.GetInventoryManager();
        PopupManager = GetComponent<PopupManager>();
        _playerGameObject = owningPlayer; // Important for knowing who to pass to item usage

        if (_localInventoryManager == null || _playerGameObject == null) {
            Debug.LogError("InventoryUIManager received null references during Initialize! UI may not function.", gameObject);
            enabled = false;
            return;
        }


        // Init managers
        UpgradeScreen.Init(this,client.UpgradeManager);
        UIManagerInventory.Init(owningPlayer,client);
        PopupManager.Init(_localInventoryManager);
    }
}