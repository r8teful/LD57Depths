using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISubUpgradeBar : MonoBehaviour {
    private UISubPanelUpgrades _parent;
    private SubRecipeSO _recipe;
    private IngredientStatus _cachedStatus;
    private int _contributed;
    private int _total;
    [SerializeField] private Image _resourceImageBig; // To the left used for what is remaining
    [SerializeField] private Image _resourceImageSmall; // In the button 
    [SerializeField] private Image _barProgressImage; // In the button 
    [SerializeField] private TextMeshProUGUI _contributedText;
    [SerializeField] private TextMeshProUGUI _totalText;
    [SerializeField] private TextMeshProUGUI _contributingButtonText;
    [SerializeField] private TextMeshProUGUI _inventoryAmountText;
    [SerializeField] private GameObject _invAmountContianer;
    [SerializeField] private Button  _contributingButton;
    [SerializeField] private Material  _progressDoneMaterial;
    private void Awake() {
        _contributingButton.onClick.AddListener(ContributeClicked);
    }

    internal void Init(UISubPanelUpgrades uISubPanelUpgrades, SubRecipeSO data, IngredientStatus ingredient, int contributed,int total) {
        _parent = uISubPanelUpgrades;
        _recipe = data;
        _cachedStatus = ingredient;
        _contributed = contributed;
        _total = total;
        gameObject.name = _cachedStatus.Item.itemName;
        Sprite sprite = _cachedStatus.Item.icon;
        if (sprite != null) {
            _resourceImageBig.sprite = sprite;
            _resourceImageSmall.sprite = sprite;
        }
        UpdateVisuals(true);
    }
    public void SetNewData(IngredientStatus ingredient, int contributed) {
        _cachedStatus = ingredient;
        _contributed = contributed;
        UpdateVisuals(false);
        // Recipe and its related parts stays the same, when they change the object just gets removed, handled by UISUbPanelUpgrades
    }
    private void ContributeClicked() {
        Instantiate(App.ResourceSystem.GetPrefab("UIParticleButton"),_contributingButton.transform.position,Quaternion.identity,NetworkedPlayer.LocalInstance.UiManager.UISubControlPanel.PanelMain)
            .transform.SetSiblingIndex(1);
        
        SubmarineManager.Instance.AttemptContribute(_recipe.ID, _cachedStatus.Item.ID, _cachedStatus.RequiredAmount);
    }
    
    private void UpdateVisuals(bool fromInit) {
        string color = _cachedStatus.HasEnough ? "white" : "red";
        _inventoryAmountText.text = $"<color=\"{color}\">{_cachedStatus.CurrentAmount}";
        if(_contributed >= 0) {
            _contributingButtonText.text = _cachedStatus.RequiredAmount.ToString();
            _contributedText.text = _contributed.ToString();
        } 
        if(_contributed >= _total) {
            if(_contributingButton.gameObject.activeSelf && !fromInit) // Another bool woo even more complicated now!
                _parent.BarCompleteAnimation(); // UGLY AND DOESN'T WORK BECAUSE WE WILl call this on INIT which is called on enable and we will always play it holy shit what a mess
            _contributingButton.onClick.RemoveListener(ContributeClicked);
            _contributingButton.gameObject.SetActive(false);
            _invAmountContianer.SetActive(false);
            _barProgressImage.material = _progressDoneMaterial;
        }

        // Progression bar visual
        if (_total > 0) {
            _totalText.text = $"/{_total}";
            float raw = (float)_contributed / _total;
            _barProgressImage.fillAmount = Mathf.Floor(raw * 10f) / 10f;
        } else { 
            _barProgressImage.fillAmount = 0;
        }
    }
}