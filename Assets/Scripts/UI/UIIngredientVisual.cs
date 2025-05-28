using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Used in the popup for inventory crafting
public class UIIngredientVisual : MonoBehaviour {
    public Image resourceIcon;
    public TextMeshProUGUI resourceAmountText;
    public TextMeshProUGUI resourceNameText;
    public TextMeshProUGUI resourceHaveText;

    internal void Init(IngredientStatus ingredient) {
        string color = ingredient.HasEnough ? "green" : "red";
        //sb.AppendLine($"<color={color}>{status.Item.itemName}: {status.CurrentAmount}/{status.RequiredAmount}</color>");
        Sprite sprite = ingredient.Item.icon;
        if (sprite != null) {
            resourceIcon.sprite = sprite;
        }
        resourceAmountText.text = ingredient.RequiredAmount.ToString();
        resourceNameText.text = ingredient.Item.itemName.ToString();
        resourceHaveText.text = ingredient.CurrentAmount.ToString();
    }
}