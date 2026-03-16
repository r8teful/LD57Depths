using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Used in the popup for inventory crafting
public class UIIngredientVisual : MonoBehaviour {
    public Image resourceIcon;
    public TextMeshProUGUI resourceAmountText;
    //public TextMeshProUGUI resourceNameText;
    public TextMeshProUGUI resourceHaveText;

    internal void Init(IngredientStatus ingredient) {
        string color = ingredient.HasEnough ? "white" : "red";
        Sprite sprite = ingredient.Item.icon;
        if (sprite != null) {
            resourceIcon.sprite = sprite;
        }
        resourceAmountText.text = $"<color=\"{color}\">{ingredient.RequiredAmount}";
        //resourceNameText.text = $"<color=\"{color}\">{ingredient.Item.itemName}";
        if (resourceHaveText == null) { // todo we're not using resource have atm so 
            resourceHaveText.gameObject.SetActive(false);
        } else {
            resourceHaveText.text = $"<color=\"{color}\">{ingredient.CurrentAmount}";

        }
    }
}