using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIUpgradeScreen : MonoBehaviour {
    private List<UIUpgradeTree> _upgradeTrees = new List<UIUpgradeTree>();
    [SerializeField] private Transform _upgradeContainerPlayer;
    [SerializeField] private Transform _upgradeContainerEnv;
    [SerializeField] private Button _buttonTreePlayer;
    [SerializeField] private Button _buttonTreeEnv;
    [SerializeField] private GameObject _upgradePanel;
    [SerializeField] private GameObject _upgradePanelPlayer;
    [SerializeField] private GameObject _upgradePanelEnv;
    private UIManager _UIManagerParent;
    public UIManager GetUIManager() => _UIManagerParent;
    private void Start() {
        _upgradePanel.SetActive(true);
        _upgradePanelPlayer.SetActive(true);
        _upgradePanelEnv.SetActive(false);
    }
    internal void Init(UIManager UIManager, UpgradeManager upgradeManager) {
        _UIManagerParent = UIManager;
        var treeData = App.ResourceSystem.UpgradeTreeData;
        var playerTrees = treeData.Where(d => (int)d.type < 4).ToList(); // Only the player upgrades
        var envTrees = treeData.Where(d => (int)d.type >= 4).ToList(); // Only the player upgrades
        foreach (var tree in playerTrees) {
            var treeObj = Instantiate(App.ResourceSystem.GetPrefab<UIUpgradeTree>("UpgradeTree"), _upgradeContainerPlayer);
            treeObj.Init(this,tree);
            treeObj.name = $"UpgradeTreePlayer_{tree.type}";
        }
        foreach (var tree in envTrees) {
            var treeObj = Instantiate(App.ResourceSystem.GetPrefab<UIUpgradeTree>("UpgradeTree"), _upgradeContainerEnv);
            treeObj.Init(this, tree);
            treeObj.name = $"UpgradeTreeEnv_{tree.type}";
        }
        _buttonTreePlayer.onClick.AddListener(OnTreePlayerButtonClick);
        _buttonTreeEnv.onClick.AddListener(OnTreeEnvButtonClick);
    }

    private void OnTreePlayerButtonClick() {
        _upgradePanelPlayer.SetActive(true);
        _upgradePanelEnv.SetActive(false);
        SetTabVisual(true);
    }
    private void OnTreeEnvButtonClick() {
        _upgradePanelPlayer.SetActive(false);
        _upgradePanelEnv.SetActive(true);
        SetTabVisual(false);
    }
    // Uggly but works lol 
    private void SetTabVisual(bool isPlayerTab) {
        // These buttons move less
        _buttonTreePlayer.GetComponent<UITabButton>().SetButtonVisual(isPlayerTab,0.4f);
        _buttonTreeEnv.GetComponent<UITabButton>().SetButtonVisual(!isPlayerTab,0.4f);
    }
    public void DEBUGShowScreen() {
        _upgradePanel.SetActive(!_upgradePanel.activeSelf);
    }
}
