using System.Collections.Generic;
using UnityEngine;

public class UIUpgradeScreen : MonoBehaviour {
    private List<UIUpgradeTree> _upgradeTrees = new List<UIUpgradeTree>();
    [SerializeField] private Transform _upgradeContainer;
    [SerializeField] private GameObject _upgradePanel;
    private UIManager _UIManagerParent;
    public UIManager GetUIManager() => _UIManagerParent;
    private void Start() {
        _upgradePanel.SetActive(true);
    }
    internal void Init(UIManager UIManager, UpgradeManager upgradeManager) {
        _UIManagerParent = UIManager;
        var treeData = App.ResourceSystem.UpgradeTreeData;
        foreach (var tree in treeData) {
            var treeObj = Instantiate(App.ResourceSystem.GetPrefab<UIUpgradeTree>("UpgradeTree"), _upgradeContainer);
            treeObj.Init(this,tree);
            treeObj.name = $"UpgradeTree_{tree.type}";
            // Should call UpgradeRecipeSO.PrepareRecipe on all recipes here!
        }
    }
    public void DEBUGShowScreen() {
        _upgradePanel.SetActive(!_upgradePanel.activeSelf);
    }
}
