using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Used in the popup for inventory crafting
public class UIIngredientVisual : MonoBehaviour {
    public Image resourceIcon;
    public TextMeshProUGUI resourceAmountText;

    internal void Init(RequiredItem ingredient) {
        Sprite sprite = ingredient.item.icon;
        if (sprite != null) {
            resourceIcon.sprite = sprite;
        }
        resourceAmountText.text = ingredient.quantity.ToString();
    }
}