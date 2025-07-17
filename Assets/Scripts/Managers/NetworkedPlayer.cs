using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

// All players have this script, this handles references and setup of relevant things all players should have
public class NetworkedPlayer : NetworkBehaviour {
    private InputManager _inputManager;
    private UpgradeManager _upgradeManager;
    private List<INetworkedPlayerModule> _modules;

    public CraftingComponent CraftingComponent { get; private set; }
    public NetworkedPlayerInventory InventoryN { get; private set; }
    public UIManager UiManager { get; private set; }
    public PlayerVisualHandler PlayerVisuals { get; private set; }
    public PlayerLayerController PlayerLayerController { get; private set; }
    public PlayerCameraController PlayerCamera { get; private set; }
    public ToolController ToolController { get; private set; }
    public static NetworkedPlayer LocalInstance { get; private set; } // Singleton for local player
    public NetworkObject PlayerNetworkedObject => base.NetworkObject; // Expose NetworkObject for other scripts to use

    public override void OnStartClient() {
        base.OnStartClient();
        // Register the player
        CacheSharedComponents();
        if (!base.IsOwner) {
            GetComponent<PlayerMovement>().enabled = false; 
            base.enabled = false;
            return;
        }
        LocalInstance = this;
        InitializePlayer();
        CmdNotifyServerOfInitialization();
    }

    // All clients have access to this
    private void CacheSharedComponents() {
        PlayerLayerController = GetComponent<PlayerLayerController>();
        PlayerVisuals = GetComponent<PlayerVisualHandler>();
    }

    private void InitializePlayer() {
        Debug.Log("Starting Player Initialization...");

        gameObject.name = $"PlayerOnline: {LocalConnection.ClientId}";
        // Add local behaviours that are required for the player.

        CraftingComponent = gameObject.AddComponent<CraftingComponent>();
        PlayerCamera = gameObject.AddComponent<PlayerCameraController>();
        _inputManager = gameObject.AddComponent<InputManager>();

        // Discover all modules on this GameObject and sort based on initialization order.
        _modules = GetComponents<INetworkedPlayerModule>().ToList();
        _modules.Sort((a, b) => a.InitializationOrder.CompareTo(b.InitializationOrder));

        // Cache core components for easy access.
        InventoryN = GetComponent<NetworkedPlayerInventory>();
        ToolController = GetComponent<ToolController>();

        // Cache core components for easy access.
        InventoryN = GetComponent<NetworkedPlayerInventory>();
        PlayerLayerController = GetComponent<PlayerLayerController>();
        PlayerVisuals = GetComponent<PlayerVisualHandler>();
        ToolController = GetComponent<ToolController>();
        _upgradeManager = UpgradeManager.Instance;

        InventoryN.Initialize(); // We have to do this first before everything else, then spawn the UI manager, and then start the other inits 
        
        // Spawn the UIManager.
        UiManager = Instantiate(App.ResourceSystem.GetPrefab<UIManager>("UIManager"));
        UiManager.Init(InventoryN.GetInventoryManager(), gameObject, _upgradeManager); // UI needs inv to suscribe to events and display it 

        // Initialize all modules in the determined order.
        foreach (var module in _modules) {
            module.Initialize(this);
            //Debug.Log($"Initialized Module: {module.GetType().Name} (Order: {module.InitializationOrder})");

        }
        Debug.Log($"Player Initialization Complete! Initialized {_modules.Count} modules");
    }
    public override void OnStopClient() {
        base.OnStopClient();
        if (UiManager != null) {
            Destroy(UiManager);
        }
        LocalInstance = null;
    }

    [ServerRpc(RequireOwnership = true)]
    private void CmdNotifyServerOfInitialization() {
        NetworkedPlayersManager.Instance.Server_RegisterPlayer(base.Owner, this);

        // This is also a great place to trigger any "local player is ready" logic

    }
}