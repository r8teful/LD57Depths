// PlayerUIController.cs
using UnityEngine;
using FishNet.Object; // For NetworkObject

public class PlayerUIController : NetworkBehaviour {
    [Header("UI Prefabs")]
    [SerializeField] private GameObject inventoryUIPrefab; // Assign your Inventory UI Prefab (with InventoryUIManager)

    private GameObject _instantiatedInventoryUI;
    private InventoryUIManager _inventoryUIManager;

    public override void OnStartClient() {
        base.OnStartClient();
        if (base.IsOwner) {
            if (inventoryUIPrefab == null) {
                Debug.LogError("InventoryUIPrefab not assigned on PlayerUIController!", gameObject);
                return;
            }

            // Instantiate the Inventory UI
            _instantiatedInventoryUI = Instantiate(inventoryUIPrefab);
            _inventoryUIManager = _instantiatedInventoryUI.GetComponent<InventoryUIManager>();

            if (_inventoryUIManager == null) {
                Debug.LogError("Instantiated Inventory UI Prefab is missing InventoryUIManager component!", _instantiatedInventoryUI);
                Destroy(_instantiatedInventoryUI); // Clean up
                return;
            }

            // Get references from THIS player object
            InventoryManager localInvManager = GetComponent<InventoryManager>();
            ItemSelectionManager itemSelectionManager = GetComponent<ItemSelectionManager>();

            if (localInvManager == null || itemSelectionManager == null) {
                Debug.LogError("Player prefab is missing InventoryManager or ItemSelectionManager!", gameObject);
                Destroy(_instantiatedInventoryUI);
                return;
            }

            // Initialize the UIManager with references from this player
            _inventoryUIManager.Init(localInvManager, itemSelectionManager, gameObject);
            Debug.Log($"Player {OwnerId}: Inventory UI Initialized.", gameObject);
        } else {
            // This is a non-owned player instance on this client. Disable UI interaction scripts if any.
            // If UIManager script itself has input listeners, they might need guarding too.
            // Or, ensure only the owner's UIManager receives input.
            // The InventoryUIManager is part of the UI prefab, so it won't be on non-owner prefabs anyway.
            // This PlayerUIController script itself should probably be disabled on non-owners.
            this.enabled = false; // Disable this controller if not owner.
        }
    }
    public override void OnStopClient() {
        base.OnStopClient();
        // Clean up instantiated UI if this player object is destroyed (and was owner)
        if (_instantiatedInventoryUI != null) {
            Destroy(_instantiatedInventoryUI);
        }
    }
}