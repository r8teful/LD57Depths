using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

public class UIUpgradeScreen : MonoBehaviour {
    [SerializeField] private GameObject _upgradePanel;
    [SerializeField] private GameObject _upgradePanelTree;
    [SerializeField] private UpgradeTreeController _upgradeTreeController;
    public UpgradePanAndZoom PanAndZoom;
    private UIManager _UIManagerParent;
    private UpgradeTreeDataSO _treeData;
    private UIUpgradeTree _upgradeTreeInstance;
    public bool IsOpen => _upgradePanel.activeSelf;
    
    public UIManager GetUIManager() => _UIManagerParent;
    public static event Action<UpgradeTreeDataSO> OnTabChanged; // Used to show correct stats 
    public static event Action<UpgradeNodeSO> OnSelectedNodeChanged; // Used to show correct stats 
    public event Action<bool> OnPanelChanged; 
    private void Start() {
        _upgradePanel.SetActive(false); // Start with the panel being hidden
        _upgradePanelTree.SetActive(true);
        UIUpgradeTree.OnUpgradeButtonPurchased += OnUpgradePurchasedThroughTree;

    }
    // todo make this good the shake is fucked it doesn't go back to where it started
    private void OnUpgradePurchasedThroughTree() {
        Debug.Log("PURSHASETREE");
        var rect = _upgradePanel.GetComponent<RectTransform>();
        rect.DOShakeAnchorPos(0.2f,60,2,180,randomnessMode:ShakeRandomnessMode.Harmonic);
       // rect.DOShakeRotation(0.4f,60,2,180,randomnessMode:ShakeRandomnessMode.Harmonic);
        //rect.DOComplete();
    }
    internal void Init(UIManager UIManager, NetworkedPlayer client) {
        _UIManagerParent = UIManager;
        _treeData = App.ResourceSystem.GetTreeByName(GameSetupManager.Instance.GetUpgradeTreeName()); // This will obviously have to come from some sort of "game selection" manager
       
        // We have to get the existing data from the UpgradeManager, for both the local player, and the communal from the server
        // I don't think we should do it here though, do it in the upgrade managers themselves, then they need to call the approriate things 
        var pUpgrades = UpgradeManagerPlayer.LocalInstance.GetUnlockedUpgrades();

        _upgradeTreeInstance = InstantiateTree(_treeData, _upgradePanelTree.transform, pUpgrades, client);
        PanAndZoom.Init(client.InputManager);
        _upgradeTreeController.Init(client, _upgradeTreeInstance);    
    }
    private UIUpgradeTree InstantiateTree(UpgradeTreeDataSO treeData, Transform transformParent, HashSet<ushort> pUpgrades, NetworkedPlayer player) {
        if (treeData == null) {
            Debug.LogError("Could not find tree!");
            return null;
        }
        if (treeData.prefab == null) {
            Debug.LogWarning($"{treeData.treeName} does not have a corresponding UI tree, skipping...");
            return null;
        }
        var treeObj = Instantiate(treeData.prefab, transformParent);
        treeObj.Init(treeData, pUpgrades, player);
        treeObj.name = $"UpgradeTree_{treeData.treeName}";
        return treeObj;
    }
    public void PanelToggle() {
        var newState = !_upgradePanel.activeSelf;
        _upgradePanel.SetActive(newState);
        if (_upgradePanel.activeSelf) {
            // we're now open
            _upgradeTreeController.OnTreeOpen();
        }
        OnPanelChanged?.Invoke(newState);
    }

    internal void PanelHide() {
        _upgradePanel.SetActive(false);
    }
}
