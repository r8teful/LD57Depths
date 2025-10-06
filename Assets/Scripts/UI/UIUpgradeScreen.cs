using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIUpgradeScreen : MonoBehaviour {
    [SerializeField] private Button _buttonTreeTool;
    [SerializeField] private Button _buttonTreePlayer;
    [SerializeField] private GameObject _upgradePanel;
    [SerializeField] private GameObject _upgradePanelTool;
    [SerializeField] private GameObject _upgradePanelPlayer;
    private UIManager _UIManagerParent;
    private UpgradeTreeDataSO _treeDataTool;
    private UpgradeTreeDataSO _treeDataPlayer;
    public UIManager GetUIManager() => _UIManagerParent;
    public static event Action<UpgradeTreeDataSO> OnTabChanged; // Used to show correct stats 
    public static event Action<UpgradeRecipeSO> OnSelectedUpgradeChanged; // Used to show correct stats 
    private void Start() {
        _upgradePanel.SetActive(false); // Start with the panel being hidden
        _upgradePanelTool.SetActive(true);
        _upgradePanelPlayer.SetActive(false);
    }
    internal void Init(UIManager UIManager, UpgradeManagerPlayer upgradeManager) {
        _UIManagerParent = UIManager;
        _treeDataTool = App.ResourceSystem.GetTreeByName("Mining Lazer"); // This will obviously have to come from some sort of "game selection" manager
        _treeDataPlayer = App.ResourceSystem.GetTreeByName("Player"); // This will obviously have to come from some sort of "game selection" manager
       
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

        InstantiateTree(_treeDataTool, _upgradePanelTool.transform, pUpgrades);
        InstantiateTree(_treeDataPlayer, _upgradePanelPlayer.transform, pUpgrades);


        _buttonTreeTool.onClick.AddListener(OnButtonToolClick);
        _buttonTreePlayer.onClick.AddListener(OnButtonPlayerClick);
    }
    private void InstantiateTree(UpgradeTreeDataSO treeData, Transform transformParent, HashSet<ushort> pUpgrades) {
        if (treeData == null) {
            Debug.LogError("Could not find tree!");
            return;
        }
        if (treeData.prefab == null) {
            Debug.LogWarning($"{treeData.treeName} does not have a corresponding UI tree, skipping...");
            return;
        }
        var treeObj = Instantiate(treeData.prefab, transformParent);
        treeObj.Init(this, treeData, pUpgrades);
        treeObj.name = $"UpgradeTree_{treeData.treeName}";
    }
    public void PanelToggle() {
        _upgradePanel.SetActive(!_upgradePanel.activeSelf);
    }

    internal void PanelHide() {
        _upgradePanel.SetActive(false);
    }
    private void OnButtonToolClick() {
        _upgradePanelPlayer.SetActive(false);
        _upgradePanelTool.SetActive(true);
        SetTabVisual(false);
        OnTabChanged?.Invoke(_treeDataTool);
    }
    private void OnButtonPlayerClick() {
        _upgradePanelPlayer.SetActive(true);
        _upgradePanelTool.SetActive(false);
        SetTabVisual(true);
        OnTabChanged?.Invoke(_treeDataPlayer);
    }
    // Uggly but works lol 
    private void SetTabVisual(bool isPlayerTab) {
        // These buttons move less
        _buttonTreePlayer.GetComponent<UITabButton>().SetButtonVisual(isPlayerTab, 0.4f);
        _buttonTreeTool.GetComponent<UITabButton>().SetButtonVisual(!isPlayerTab, 0.4f);
    }

    internal void OnUpgradeNodeClicked(UpgradeRecipeSO upgradeData) {
        OnSelectedUpgradeChanged?.Invoke(upgradeData);
    }
}
