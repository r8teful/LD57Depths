using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISubUpgradeBar : MonoBehaviour {
    private ItemData _mainItem;
    [SerializeField] private Image _resourceImageBig; // To the left used for what is remaining
    [SerializeField] private Image _resourceImageSmall; // In the button 
    [SerializeField] private TextMeshProUGUI _remainingText;
    [SerializeField] private TextMeshProUGUI _contributingButtonText;
    [SerializeField] private TextMeshProUGUI _inventoryAmountText;
    private void Start() {
       // Subscribe to upgrade change, then update visuals accordingly
    }
    internal void Init(IngredientStatus ingredient, int totalLeft) {
        string color = ingredient.HasEnough ? "white" : "red";
        Sprite sprite = ingredient.Item.icon;
        if (sprite != null) {
            _resourceImageBig.sprite = sprite;
            _resourceImageSmall.sprite = sprite;
        }
        gameObject.name = ingredient.Item.itemName;
        _contributingButtonText.text = ingredient.RequiredAmount.ToString();
        _inventoryAmountText.text = $"<color=\"{color}\">{ingredient.CurrentAmount}";
        _remainingText.text = totalLeft.ToString();
    }
}