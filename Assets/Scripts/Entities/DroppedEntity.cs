using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class DroppedEntity : NetworkBehaviour {
    // SyncVars automatically synchronize from server to clients
    private readonly SyncVar<ushort> _itemID = new SyncVar<ushort>(new SyncTypeSettings(ReadPermission.Observers)); 
    private readonly SyncVar<int> _quantity = new SyncVar<int>(new SyncTypeSettings(ReadPermission.Observers));
    

    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float pickupDelay = 0.5f; // Prevent insta-pickup after drop

    private float timeSinceSpawned = 0f;
    private ItemData _cachedItemData = null;

    public SyncVar<ushort> ItemID => _itemID;
    public SyncVar<int> Quantity => _quantity;
    public bool CanPickup => timeSinceSpawned >= pickupDelay;
    //public ItemData ItemData => _cachedItemData;
    // --- Cached ItemData (lookup result, not synced) ---

    void Update() {
        if (timeSinceSpawned < pickupDelay) {
            timeSinceSpawned += Time.deltaTime;
        }
    }

    private void Awake() {
        _quantity.OnChange += OnQuantityChanged;
        _itemID.OnChange += OnItemIDChanged;
    }
    public override void OnStartServer() {
        base.OnStartServer();
        // Server should initialize this immediately after spawning
        // We'll do this from the Player's drop logic
        // Example: GetComponent<WorldItem>().ServerInitialize(data, quant);
    }

    public override void OnStartClient() {
        base.OnStartClient();
        if (_itemID.Value != ResourceSystem.InvalidID) {
            // If ID is valid on start, force an initial lookup and visual update
            OnItemIDChanged(_itemID.Value, _itemID.Value, IsServerStarted); // Pass same value to trigger update
        } else {
            // If ID is invalid, ensure visuals are cleared
            UpdateVisuals(null, _quantity.Value);
        }
    }

    // Called on SERVER when spawning the item
    [Server] // Ensures this only runs on the server
    public void ServerInitialize(ushort id, int quantity, bool fromBlock) {
        if (id == ResourceSystem.InvalidID || quantity <= 0) {
            Debug.LogError("ServerInitialize called with invalid data.", this.gameObject);
            // Despawn immediately if invalid
            ServerManager.Despawn(gameObject);
            return;
        }
        // Look up ItemData on server side for validation / logic if needed
        _cachedItemData = App.ResourceSystem.GetItemByID(id);
        if (_cachedItemData == null) {
            Debug.LogError($"Server could not find ItemData for ID {id} in database! Despawning.", this.gameObject);
            ServerManager.Despawn(gameObject);
            return;
        }


        // Set the SyncVars
        _itemID.Value= id;
        _quantity.Value = quantity;
        timeSinceSpawned = 0f;

        Debug.Log($"[Server] Initialized WorldItem {gameObject.name} with ID {_itemID} ({_cachedItemData.name}) x{_quantity}");
    }

    // SyncVar hook for Quantity (only called on clients when server changes value)
    private void OnQuantityChanged(int prev, int next, bool asServer) {
        if (asServer) return; // Server already knows
        // Update visuals if the quantity changes (though unlikely for simple drops)
        UpdateVisuals(_cachedItemData, next); // Use the current ItemData
    }
    // Called on CLIENTS when server changes _itemID
    private void OnItemIDChanged(ushort prevID, ushort nextID, bool asServer) {
        if (asServer) return; // Server already knows

        Debug.Log($"[Client] WorldItem {gameObject.name} ItemID changed from {prevID} to {nextID}. Performing lookup.");
        _cachedItemData = App.ResourceSystem.GetItemByID(nextID); // Perform lookup
        UpdateVisuals(_cachedItemData, _quantity.Value); // Update visuals with new data
    }

    // --- Visuals & Interaction ---
    private void UpdateVisuals(ItemData data, int quantity) {
        if (spriteRenderer == null) return;

        if (data != null && quantity > 0) {
            spriteRenderer.enabled = true;
        } else {
            spriteRenderer.enabled = false;
        }
    }
}
