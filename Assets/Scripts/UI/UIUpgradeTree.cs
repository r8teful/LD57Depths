using System;
using TMPro;
using UnityEngine;

public class UIUpgradeTree : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private Transform _nodeContainer;
    internal void Init(UIUpgradeScreen uIUpgradeScreen, UpgradeTreeDataSO tree) {
        _text.text = tree.type.ToString();
        // If its not linear we'll have to somehow make it dynamically here
        for (int i = 0; i < tree.UpgradeTree.Keys.Count; i++) {
            // Keys are the levels so we just start at level 0
            //tree.UpgradeTree[i].PrepareRecipe(i,tree.costsValues);
            var node = Instantiate(App.ResourceSystem.GetPrefab<UIUpgradeNode>("UpgradeNode"),_nodeContainer);
            node.name = $"UpgradeNode_{tree.type}_LVL_{i}";
            node.Init(tree.UpgradeTree[i]);
            uIUpgradeScreen.GetUIManager().PopupManager.RegisterIPopupInfo(node); // Oh my god what a way to do this but I guess it makes sence
        }
    }
}

