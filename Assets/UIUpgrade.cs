using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class UIUpgrade : MonoBehaviour {
    public TextMeshProUGUI nameString;
    public Transform CostContainer;
    public UIResourceElement ResourceElement;
    internal void Init(UpgradeDataSO upgrade) {
        var name = Regex.Replace(upgrade.type.ToString(), "([a-z])([A-Z])", "$1 $2");
        nameString.text = $"{name}<color=\"blue\">(lvl{UpgradeManager.Instance.GetUpgradeLevel(upgrade.type)})";
        var d = UpgradeManager.Instance.GetUpgradeCost(upgrade.type);
        foreach(var cost in d) {
            Instantiate(ResourceElement, CostContainer).Init(cost.Key, cost.Value);
        }
    }
}