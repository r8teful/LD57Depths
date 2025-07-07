using System.Collections.Generic;
using UnityEngine;

public class UIUpgradeScreen : MonoBehaviour {
    private List<UIUpgradeTree> _upgradeTrees = new List<UIUpgradeTree>();
    [SerializeField] private Transform _upgradeContainer;
    [SerializeField] private GameObject _upgradePanel;
    private UIManager _UIManagerParent;
    public UIManager GetUIManager() => _UIManagerParent;
    private void Start() {
        _upgradePanel.SetActive(false);
    }
    internal void Init(UpgradeRecipeSO upgrade,UIManager UIManager) {
        var treeData = App.ResourceSystem.UpgradeTreeData;
        _UIManagerParent = UIManager;
        foreach(var tree in treeData) {
            var treeObj = Instantiate(App.ResourceSystem.GetPrefab<UIUpgradeTree>("UpgradeTree"), _upgradeContainer);
            treeObj.Init(this,tree);

            // Should call UpgradeRecipeSO.PrepareRecipe on all recipes here!
        }
    }
}
