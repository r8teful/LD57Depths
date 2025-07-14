using FishNet.Object;
using System.Collections;
using UnityEngine;

// All players have this script, this handles references and setup of relevant things all players should have
public class NetworkedPlayer : NetworkBehaviour {
    private UIManager _uiManager;
    private NetworkedPlayerInventory _inventoryN;
    private InputManager _inputManager;
    private UpgradeManager _upgradeManager;
    private CraftingComponent _crafting;
    public PlayerVisualHandler PlayerVisuals { get; private set; }
    public ToolController ToolController { get; private set; }
    public static NetworkedPlayer LocalInstance { get; private set; } // Singleton for local player
    public NetworkObject PlayerNetworkedObject => base.NetworkObject; // Expose NetworkObject for other scripts to use

    public override void OnStartClient() {
        base.OnStartClient();
        if (!base.IsOwner) {
            GetComponent<PlayerMovement>().enabled = false; 
            base.enabled = false;
            return;
        }
        LocalInstance = this;
        _inventoryN = GetComponent<NetworkedPlayerInventory>();

        _crafting = gameObject.AddComponent<CraftingComponent>();
        PlayerVisuals = GetComponent<PlayerVisualHandler>();
        ToolController = GetComponent<ToolController>();
        _upgradeManager = UpgradeManager.Instance;
        StartCoroutine(StartRoutine());
        
    }
    private IEnumerator StartRoutine() {
        // Wait until inventory is setup
        yield return new WaitUntil(() => _inventoryN.OnStartClientCalled);
        _uiManager = Instantiate(App.ResourceSystem.GetPrefab<UIManager>("UIManager"));
        _crafting.Init(_inventoryN.GetInventoryManager()); // Crafting needs player inventory obviously
        _uiManager.Init(_inventoryN.GetInventoryManager(), gameObject,_upgradeManager); // UI needs inv to suscribe to events and display it 
        _upgradeManager.Init(_crafting); // UpgradeManager needs crafting component for checking recipes
        _inputManager = gameObject.AddComponent<InputManager>();
        _inputManager.Init(_uiManager.UIManagerInventory, this,ToolController);
        GetComponent<PlayerMovement>().Init(); 
    }
    public override void OnStopClient() {
        base.OnStopClient();
        if (_uiManager != null) {
            Destroy(_uiManager);
        }
        LocalInstance = null;
    }
}