using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

public class UIUpgrade : MonoBehaviour {
    public TextMeshProUGUI nameString;
    public Transform CostContainer;
    public TextMeshProUGUI IncreaseString;
    public UIResourceElement ResourceElement;
    internal void Init(UpgradeDataSO upgrade) {
        var name = Regex.Replace(upgrade.type.ToString(), "([a-z])([A-Z])", "$1 $2");
        var lvl = UpgradeManager.Instance.GetUpgradeLevel(upgrade.type);
        if(lvl >= upgrade.maxLevel) {
            GetComponentInChildren<BuyButton>().gameObject.SetActive(false);
            IncreaseString.text = "";
            nameString.text = $"{name}<color=\"purple\">(lvl{lvl})";
            return;
        }
        nameString.text = $"{name}<color=\"blue\">(lvl{lvl})";
        if (upgrade.increaseType == IncreaseType.Add) {
            IncreaseString.text = $"+{upgrade.increasePerLevel}";
        } else if (upgrade.increaseType == IncreaseType.Multiply) { 
            IncreaseString.text = $"+{upgrade.increasePerLevel * 100}%";
        }
        var d = UpgradeManager.Instance.GetUpgradeCost(upgrade.type);
        foreach(var cost in d) {
            Instantiate(ResourceElement, CostContainer).Init(cost.Key, cost.Value);
        }
        GetComponentInChildren<BuyButton>().type = upgrade.type;
    }
}