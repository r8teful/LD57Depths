using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISubUpgradeBar : MonoBehaviour {
    private SubRecipeSO _recipe;
    private IngredientStatus _cachedStatus;
    private int _totalLeft;
    [SerializeField] private Image _resourceImageBig; // To the left used for what is remaining
    [SerializeField] private Image _resourceImageSmall; // In the button 
    [SerializeField] private TextMeshProUGUI _remainingText;
    [SerializeField] private TextMeshProUGUI _contributingButtonText;
    [SerializeField] private TextMeshProUGUI _inventoryAmountText;
    [SerializeField] private Button  _contributingButton;
    private void Awake() {
        _contributingButton.onClick.AddListener(ContributeClicked);
    }

    internal void Init(SubRecipeSO data, IngredientStatus ingredient, int totalLeft) {
        _recipe = data;
        _cachedStatus = ingredient;
        _totalLeft = totalLeft;
        gameObject.name = _cachedStatus.Item.itemName;
        Sprite sprite = _cachedStatus.Item.icon;
        if (sprite != null) {
            _resourceImageBig.sprite = sprite;
            _resourceImageSmall.sprite = sprite;
        }
        UpdateVisuals();
    }
    public void SetNewData(IngredientStatus ingredient, int totalLeft) {
        _cachedStatus = ingredient;
        _totalLeft = totalLeft;
        UpdateVisuals();
        // Recipe and its related parts stays the same, when they change the object just gets removed, handled by UISUbPanelUpgrades
    }
    private void ContributeClicked() {
        SubmarineManager.Instance.RpcContributeToUpgrade(_recipe.ID, _cachedStatus.Item.ID, _cachedStatus.RequiredAmount,NetworkedPlayer.LocalInstance);
    }
    
    private void UpdateVisuals() {
        string color = _cachedStatus.HasEnough ? "white" : "red";
        _contributingButtonText.text = _cachedStatus.RequiredAmount.ToString();
        _inventoryAmountText.text = $"<color=\"{color}\">{_cachedStatus.CurrentAmount}";
        _remainingText.text = _totalLeft.ToString();
    }
}