using System;
using System.Collections.Generic;
using UnityEngine;

public class UIUpgradeScreen : MonoBehaviour {
    [SerializeField] private GameObject _upgradePanel;
    [SerializeField] private GameObject _upgradePanelTree;
    public UpgradePanAndZoom PanAndZoom;
    private UIManager _UIManagerParent;
    private UpgradeTreeDataSO _treeDataTool;
    private UIUpgradeTree _upgradeTreeInstance;
    public UIManager GetUIManager() => _UIManagerParent;
    public static event Action<UpgradeTreeDataSO> OnTabChanged; // Used to show correct stats 
    public static event Action<UpgradeNodeSO> OnSelectedNodeChanged; // Used to show correct stats 
    private void Start() {
        _upgradePanel.SetActive(false); // Start with the panel being hidden
        _upgradePanelTree.SetActive(true);
    }
    internal void Init(UIManager UIManager, NetworkedPlayer client) {
        _UIManagerParent = UIManager;
        _treeDataTool = App.ResourceSystem.GetTreeByName(GameManager.LocalInstance.GetUpgradeTreeName()); // This will obviously have to come from some sort of "game selection" manager
        //_treeDataTool = App.ResourceSystem.GetTreeByName("Mining Lazer"); // This will obviously have to come from some sort of "game selection" manager
        //_treeDataPlayer = App.ResourceSystem.GetTreeByName("Player"); // This will obviously have to come from some sort of "game selection" manager
       
        // We have to get the existing data from the UpgradeManager, for both the local player, and the communal from the server
        // I don't think we should do it here though, do it in the upgrade managers themselves, then they need to call the approriate things 
        var pUpgrades = UpgradeManagerPlayer.LocalInstance.GetUnlockedUpgrades();


        // Idea first was to have an upgrade tree for each "stat", but for now we just have for player, and for the tool
        //foreach (var tree in treeData) {
        //    if(tree.prefab == null) {
        //        Debug.LogWarning($"{tree.treeName} does not have a corresponding UI tree, skipping...");
        //        continue;
        //    }
        //    var treeObj = Instantiate(tree.prefab, _upgradePanelTool.transform);
        //    treeObj.Init(this, tree, pUpgrades);
        //    treeObj.name = $"UpgradeTreePlayer_{tree.treeName}";
        //}

        _upgradeTreeInstance = InstantiateTree(_treeDataTool, _upgradePanelTree.transform, pUpgrades);
        PanAndZoom.Init(client.InputManager);
        //InstantiateTree(_treeDataPlayer, _upgradePanelPlayer.transform, pUpgrades);
    }
    private UIUpgradeTree InstantiateTree(UpgradeTreeDataSO treeData, Transform transformParent, HashSet<ushort> pUpgrades) {
        if (treeData == null) {
            Debug.LogError("Could not find tree!");
            return null;
        }
        if (treeData.prefab == null) {
            Debug.LogWarning($"{treeData.treeName} does not have a corresponding UI tree, skipping...");
            return null;
        }
        var treeObj = Instantiate(treeData.prefab, transformParent);
        treeObj.Init(this, treeData, pUpgrades);
        treeObj.name = $"UpgradeTree_{treeData.treeName}";
        return treeObj;
    }
    public void PanelToggle() {
        _upgradePanel.SetActive(!_upgradePanel.activeSelf);
    }

    internal void PanelHide() {
        _upgradePanel.SetActive(false);
    }
}
