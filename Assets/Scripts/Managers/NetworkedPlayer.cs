using FishNet.Object;
using System.Collections;
using UnityEngine;

// All players have this script, this handles references and setup of relevant things all players should have
public class NetworkedPlayer : NetworkBehaviour {
    [SerializeField] private UIManager inventoryUIPrefab; // Players spawn their own UIs 
    private UIManager _uiManager;
    private NetworkedPlayerInventory _inventoryN;
    private InputManager _inputManager;
    public override void OnStartClient() {
        base.OnStartClient();
        if (base.IsOwner) {
            _inventoryN = GetComponent<NetworkedPlayerInventory>();
            _inputManager = GetComponent<InputManager>();
            StartCoroutine(StartRoutine());
        } else {
            base.enabled = false;
        }
    }
    private IEnumerator StartRoutine() {
        // Wait until inventory is setup
        yield return new WaitUntil(() => _inventoryN.OnStartClientCalled);
        _uiManager = Instantiate(inventoryUIPrefab);
        _uiManager.Init(_inventoryN.GetInventoryManager(), gameObject);
        _inputManager.SetUIManager(_uiManager.UIManagerInventory);
    }
    public override void OnStopClient() {
        base.OnStopClient();
        if (_uiManager != null) {
            Destroy(_uiManager);
        }
    }
}