using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class UIUpgradeTree : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private Transform _nodeContainer;
    [SerializeField] private Transform _resourceContainer; // For the first upgrade that is there
    internal void Init(UIUpgradeScreen uIUpgradeScreen, UpgradeTreeDataSO tree, HashSet<ushort> existingUpgrades) {
        // Makes "PlayerSpeed" become "Player Speed"  
        _text.text = Regex.Replace(tree.type.ToString(), "([a-z])([A-Z])", "$1 $2");
        var nodePrefab = App.ResourceSystem.GetPrefab<UIUpgradeNode>("UpgradeNode");
        // If its not linear we'll have to somehow make it dynamically here
        for (int i = 0; i < tree.UpgradeTree.Keys.Count; i++) {
            // Keys are the levels so we just start at level 0
            //tree.UpgradeTree[i].PrepareRecipe(i,tree.costsValues);
            var node = Instantiate(nodePrefab, _nodeContainer);
            var upgradeRecipe = tree.UpgradeTree[i];
            node.name = $"UpgradeNode_{tree.type}_LVL_{i}";

            node.Init(upgradeRecipe, this, i!=0 && (1+i)%4 == 0, isNetworked: IsNetworkedTreeType(tree));
            uIUpgradeScreen.GetUIManager().PopupManager.RegisterIPopupInfo(node); // Oh my god what a way to do this but I guess it makes sence
        }
    }

    private bool IsNetworkedTreeType(UpgradeTreeDataSO tree) {
        return tree.type == UpgradeTreeType.TreeFarm ||
               tree.type == UpgradeTreeType.Pollution ||
               tree.type == UpgradeTreeType.Lamp; 
    }

    internal void SetNodeAvailable(UpgradeRecipeBase upgradeData) {
        foreach(Transform child in _resourceContainer) {
            Destroy(child.gameObject);
        }
        foreach (var item in upgradeData.requiredItems) {
            Instantiate(App.ResourceSystem.GetPrefab<UIUpgradeResourceDisplay>("UpgradeResourceDisplay"), _resourceContainer)
                .Init(item.item.icon, item.quantity);
        }
        _resourceContainer.gameObject.SetActive(true);
    }


    // We just stay with 530 total width for now, could expand it if needed later
    void SetText(string txt) {
        _text.text = txt;
        _text.ForceMeshUpdate(); // Make sure layout info is up-to-date

        float preferredWidth = _text.preferredWidth;

        RectTransform rt = _text.rectTransform;
        Vector2 size = rt.sizeDelta;
        size.x = preferredWidth + 20f; // 20 for some extra padding
        rt.sizeDelta = size;
    }
}

