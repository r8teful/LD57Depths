﻿using FishNet.Object;
using System.Collections;
using UnityEngine;

// All players have this script, this handles references and setup of relevant things all players should have
public class NetworkedPlayer : NetworkBehaviour {
    [SerializeField] private UIManager inventoryUIPrefab; // Players spawn their own UIs 
    private UIManager _uiManager;
    private NetworkedPlayerInventory _inventoryN;
    private InputManager _inputManager;
    private UpgradeManager _upgradeManager;
    private CraftingComponent _crafting;
    public override void OnStartClient() {
        base.OnStartClient();
        if (base.IsOwner) {
            _inventoryN = GetComponent<NetworkedPlayerInventory>();
            _inputManager = GetComponent<InputManager>();
            _crafting = GetComponent<CraftingComponent>();
            _upgradeManager = UpgradeManager.Instance;
            StartCoroutine(StartRoutine());
        } else {
            base.enabled = false;
        }
    }
    private IEnumerator StartRoutine() {
        // Wait until inventory is setup
        yield return new WaitUntil(() => _inventoryN.OnStartClientCalled);
        _uiManager = Instantiate(inventoryUIPrefab);
        _crafting.Init(_inventoryN.GetInventoryManager()); // Crafting needs player inventory obviously
        _uiManager.Init(_inventoryN.GetInventoryManager(), gameObject,_upgradeManager); // UI needs inv to suscribe to events and display it 
        _upgradeManager.Init(_crafting); // UpgradeManager needs crafting component for checking recipes
        _inputManager.SetUIManager(_uiManager.UIManagerInventory);
    }
    public override void OnStopClient() {
        base.OnStopClient();
        if (_uiManager != null) {
            Destroy(_uiManager);
        }
    }
}