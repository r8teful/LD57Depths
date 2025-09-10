using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIUpgradeScreen : MonoBehaviour {
    [SerializeField] private Transform _upgradeContainerPlayer;
    [SerializeField] private Button _buttonTreePlayer;
    [SerializeField] private GameObject _upgradePanel;
    [SerializeField] private GameObject _upgradePanelPlayer;
    private UIManager _UIManagerParent;
    public UIManager GetUIManager() => _UIManagerParent;
    private void Start() {
        _upgradePanel.SetActive(false);
        _upgradePanelPlayer.SetActive(true);
    }
    internal void Init(UIManager UIManager, UpgradeManagerPlayer upgradeManager) {
        _UIManagerParent = UIManager;
        var treeData = App.ResourceSystem.UpgradeTreeData;
        // We have to get the existing data from the UpgradeManager, for both the local player, and the communal from the server
        // I don't think we should do it here though, do it in the upgrade managers themselves, then they need to call the approriate things 
        var pUpgrades = UpgradeManagerPlayer.Instance.GetUnlockedUpgrades();
        foreach (var tree in treeData) {
            if(tree.prefab == null) {
                Debug.LogWarning($"{tree.treeName} does not have a corresponding UI tree, skipping...");
                continue;
            }
            var treeObj = Instantiate(tree.prefab, _upgradeContainerPlayer);
            treeObj.Init(this, tree, pUpgrades);
            treeObj.name = $"UpgradeTreePlayer_{tree.treeName}";
        }
    }
  
    public void PanelToggle() {
        _upgradePanel.SetActive(!_upgradePanel.activeSelf);
    }

    internal void PanelHide() {
        _upgradePanel.SetActive(false);
    }
}
