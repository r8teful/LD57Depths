using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIUpgradeNode : MonoBehaviour, IPopupInfo, IPointerEnterHandler, IPointerExitHandler {
    [SerializeField] private Button _buttonBig;
    [SerializeField] private Button _buttonSmall;
    [SerializeField] private Button _buttonCurrent;
    [SerializeField] private Image _imageCurrent;
    private RectTransform _rectTransform;
    private UpgradeRecipeSO _upgradeData;
    private UIUpgradeTree _treeParent;
    public bool IsBig;
    public event Action PopupDataChanged;
    public event Action<IPopupInfo, bool> OnPopupShow;

    internal void Init(UpgradeRecipeSO upgradeRecipeSO, UIUpgradeTree parent, bool isBig) {
        _treeParent = parent;
        _upgradeData = upgradeRecipeSO;
        _rectTransform = GetComponent<RectTransform>();
        IsBig = isBig;
        if(IsBig && _buttonBig != null) {
            _buttonBig.onClick.RemoveAllListeners();
            _buttonBig.onClick.AddListener(OnUpgradeButtonClicked);
            _buttonSmall.gameObject.SetActive(false);
            _buttonCurrent = _buttonBig;
            _imageCurrent = _buttonCurrent.targetGraphic.gameObject.GetComponent<Image>(); // omg so uggly
            var r = _rectTransform.sizeDelta;
            r.x = 120f;
            _rectTransform.sizeDelta = r;
        } else if(!IsBig && _buttonSmall != null) {
            _buttonSmall.onClick.RemoveAllListeners();
            _buttonSmall.onClick.AddListener(OnUpgradeButtonClicked);
            _buttonCurrent = _buttonSmall;
            _imageCurrent = _buttonCurrent.targetGraphic.gameObject.GetComponent<Image>(); // omg so uggly
            _buttonBig.gameObject.SetActive(false);
            var r = _rectTransform.sizeDelta;
            r.x = 65f;
            _rectTransform.sizeDelta = r;
        }
        UpdateVisualState();
    }
    private void OnEnable() {
        // Subscribe to the event when the object becomes active
        UpgradeManager.OnUpgradePurchased += HandleUpgradePurchased;
    }

    private void OnDisable() {
        // IMPORTANT: Always unsubscribe to prevent memory leaks
        UpgradeManager.OnUpgradePurchased -= HandleUpgradePurchased;
    }

    public void OnPointerEnter(PointerEventData eventData) {
        OnPopupShow?.Invoke(this, true);
    }

    public void OnPointerExit(PointerEventData eventData) {
        OnPopupShow?.Invoke(this, false);
    }
    private void OnUpgradeButtonClicked() {
        // UICraftingManager.Instance.AttemptCraft(upgradeData, null, null);
        UpgradeManager.Instance.PurchaseUpgrade(_upgradeData);
    }
    // This method is called by the event from the UpgradeManager
    private void HandleUpgradePurchased(UpgradeRecipeSO purchasedRecipe) {
        // When any upgrade is purchased, re-evaluate our state.
        // This is important for unlocking nodes when a prerequisite is met.
        UpdateVisualState();
    }

    // The core logic for how this node should look based on game state
    private void UpdateVisualState() {
        if (_upgradeData == null)
            return;

        bool isPurchased = UpgradeManager.Instance.IsUpgradePurchased(_upgradeData);

        if (isPurchased) {
            // State: Purchased
            var c = _buttonCurrent.colors;
            c.disabledColor = Color.white;
            _buttonCurrent.colors = c;
            _buttonCurrent.interactable = false;
            _imageCurrent.sprite = App.ResourceSystem.GetSprite($"UpgradeNode{(IsBig ? "Big" : "Small")}Purchased");
            //GetComponent<Image>().color = Color.green;
        } else {
            bool prerequisitesMet = UpgradeManager.Instance.ArePrerequisitesMet(_upgradeData);
            if (prerequisitesMet) {
                // Available
                _buttonCurrent.interactable = true;
                _treeParent.SetNodeAvailable(_upgradeData);
            } else {
                _buttonCurrent.interactable = false;
               // GetComponent<Image>().color = Color.gray;
            }
        }
    }
    public PopupData GetPopupData(InventoryManager clientInv) {
        return new PopupData(_upgradeData.displayName, _upgradeData.description, _upgradeData.GetIngredientStatuses(clientInv));
        //return null;
    }
}
