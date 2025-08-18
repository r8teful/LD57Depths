using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class UIUpgradeScreen : MonoBehaviour {
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
        _upgradePanel.SetActive(false);
        _upgradePanelPlayer.SetActive(true);
        _upgradePanelEnv.SetActive(false);
    }
    internal void Init(UIManager UIManager, UpgradeManagerPlayer upgradeManager) {
        _UIManagerParent = UIManager;
        var treeData = App.ResourceSystem.UpgradeTreeData;
        var playerTrees = treeData.Where(d => (int)d.type < 4).ToList(); // Only the player upgrades
        var envTrees = treeData.Where(d => (int)d.type >= 4).ToList(); // Only the environment upgrades
        var UItreePrefab = App.ResourceSystem.GetPrefab<UIUpgradeTree>("UpgradeTree");

        // We have to get the existing data from the UpgradeManager, for both the local player, and the communal from the server
        // I don't think we should do it here though, do it in the upgrade managers themselves, then they need to call the approriate things 
        var pUpgrades = UpgradeManagerPlayer.Instance.GetUnlockedUpgrades();
        var cUpgrades = UpgradeManagerCommunal.Instance.GetUnlockedUpgrades(); // Pulled from server
        foreach (var tree in playerTrees) {
            var treeObj = Instantiate(UItreePrefab, _upgradeContainerPlayer);
            treeObj.Init(this, tree, pUpgrades);
            treeObj.name = $"UpgradeTreePlayer_{tree.type}";
        }
        foreach (var tree in envTrees) {
            var treeObj = Instantiate(UItreePrefab, _upgradeContainerEnv);
            treeObj.Init(this, tree, cUpgrades);
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
