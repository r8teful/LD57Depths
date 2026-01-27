using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// All players have this script, this handles references and setup of relevant things all players should have
public class NetworkedPlayer : NetworkBehaviour {
    private List<INetworkedPlayerModule> _modules;
    private PlayerUISpawner _uiSpawner;
    public UpgradeManagerPlayer UpgradeManager { get; private set; }
    public InputManager InputManager { get; private set; }
    public CraftingComponent CraftingComponent { get; private set; }
    public NetworkedPlayerInventory InventoryN { get; private set; }
    public UIManager UiManager => _uiSpawner.UiManager; // Expose UIManager from the PlayerUISpawner
    public PlayerVisualHandler PlayerVisuals { get; private set; }
    public PlayerLayerController PlayerLayerController { get; private set; }
    public PlayerMovement PlayerMovement { get; private set; }
    public static NetworkedPlayer LocalInstance { get; private set; } // Singleton for local player
    public PlayerStatsManager PlayerStats { get; private set; } 
    public OxygenManager OxygenManager { get; private set; }
    public PlayerAbilities PlayerAbilities { get; private set; } 
    public PlayerRewardManager PlayerReward { get; private set; } 
    public PopupManager PopupManager => UiManager.PopupManager;
    public InventoryManager GetInventory() => InventoryN.GetInventoryManager();
    public Vector3 GetWorldPosition => transform.position;
    // World manager should be in some kind of gamemanager or something but eh just get it like this
    private WorldManager _worldManager;
    public bool IsInitialized => _isInitialized;
    private bool _isInitialized = false;

    // To avoid any wierd bugs, this should be the only OnStartClient on the player
    public override void OnStartClient() {
        base.OnStartClient();
        Debug.Log("Start Client on: " + OwnerId + " Are we owner?: " + IsOwner);
        CacheSharedComponents();
        //NetworkedPlayersManager.OnPlayersListChanged += OnPlayersChanged;
        if (!base.IsOwner) {
            GetComponent<PlayerMovement>().enabled = false;
            PlayerVisuals.InitializeOnNotOwner(this);
            return;
        }
        _worldManager = FindFirstObjectByType<WorldManager>();
        LocalInstance = this;
        InitializePlayer();
        _isInitialized = true;
        // Subscribe to other clients joining so we can properly initialize our local version of them

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        DEBUGManager.Instance.RegisterOwningPlayer(this);
#endif
    }
    public override void OnStopClient() {
        base.OnStopClient();
        if (UiManager != null) {
            Destroy(UiManager);
        }
        LocalInstance = null;
    }
    // All clients have access to this
    private void CacheSharedComponents() {
        PlayerLayerController = GetComponent<PlayerLayerController>(); // To hide and unhide players when they enter submarine
        PlayerVisuals = GetComponent<PlayerVisualHandler>(); // Because of visuals lol
        PlayerStats = GetComponent<PlayerStatsManager>(); // Because of visuals related to stats
        UpgradeManager = GetComponent<UpgradeManagerPlayer>(); // Because of visuals related to specific upgrade purchases
    }

    private void InitializePlayer() {
        Debug.Log("Starting Player Initialization...");

        gameObject.name = $"PlayerOnline: {LocalConnection.ClientId}";
        // Add local behaviours that are required for the player.

        CraftingComponent = gameObject.AddComponent<CraftingComponent>();
        InputManager = gameObject.AddComponent<InputManager>();
        // Discover all modules on this GameObject and sort based on initialization order.
        _modules = GetComponents<INetworkedPlayerModule>().ToList();
        _modules.Sort((a, b) => a.InitializationOrder.CompareTo(b.InitializationOrder));


        // Cache core components for easy access.
        InventoryN = GetComponent<NetworkedPlayerInventory>();
        _uiSpawner = GetComponent<PlayerUISpawner>();
        InventoryN = GetComponent<NetworkedPlayerInventory>();
        PlayerLayerController = GetComponent<PlayerLayerController>();
        PlayerVisuals = GetComponent<PlayerVisualHandler>();
        PlayerAbilities = GetComponent<PlayerAbilities>();
        OxygenManager = GetComponent<OxygenManager>();
        PlayerReward = GetComponent<PlayerRewardManager>();
        PlayerMovement = GetComponent<PlayerMovement>();

        InventoryN.Initialize(); // We have to do this first before everything else, then spawn the UI manager, and then start the other inits 
        
        // Initialize all modules in the determined order.
        foreach (var module in _modules) {
            module.InitializeOnOwner(this);
            //Debug.Log($"Initialized Module: {module.GetType().Name} (Order: {module.InitializationOrder})");

        }
        
        Debug.Log($"Player Initialization Complete! Initialized {_modules.Count} modules");
    }
   

    public string GetPlayerName() {
        return $"Player: {OwnerId}"; // TODO, get steam name or user defined name etc..
    }

    [ServerRpc(RequireOwnership = true)]
    internal void CmdRequestDamageTile(Vector3Int cellPos, float damage) {
        if (_worldManager == null)
            _worldManager = FindFirstObjectByType<WorldManager>();
        _worldManager.RequestDamageTile(cellPos, damage);
    }
    [ServerRpc(RequireOwnership = true)]
    public void CmdRequestDamageTile(Vector3 worldPos, float damageAmount) {
        // TODO: Server-side validation (range, tool, cooldowns, etc.)
        //Debug.Log($"Requesting damage worldPos {worldPos}, damageAmount {damageAmount}");
        // Pass request to WorldGenerator for processing
        // TODO somehow _worldmanager is null here and it cant find it 
        if (_worldManager == null)
            _worldManager = FindFirstObjectByType<WorldManager>();
        _worldManager.RequestDamageTile(worldPos, damageAmount);
    }
    [ServerRpc(RequireOwnership = true)]
    public void CmdRequestDamageNearestSolidTile(Vector3 worldPosition, float dmg, int searchRadius = 3) {
        if (_worldManager == null)
            _worldManager = FindFirstObjectByType<WorldManager>();
        _worldManager.RequestDamageNearestSolidTile(worldPosition, dmg, searchRadius);
    }
}