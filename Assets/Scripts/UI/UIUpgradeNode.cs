using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIUpgradeNode : MonoBehaviour, IPopupInfo, IPointerEnterHandler, IPointerExitHandler {
    [SerializeField] private Button _buttonBig;
    [SerializeField] private Button _buttonSmall;
    [SerializeField] private Button _buttonCurrent;
    [SerializeField] private Transform _resourceContainer;
    private RectTransform _rectTransform;
    private UpgradeRecipeSO upgradeData;
    public bool IsBig;
    public event Action PopupDataChanged;
    public event Action<IPopupInfo, bool> OnPopupShow;

    internal void Init(UpgradeRecipeSO upgradeRecipeSO,bool isBig) {
        _resourceContainer.gameObject.SetActive(false);
        upgradeData = upgradeRecipeSO;
        _rectTransform = GetComponent<RectTransform>();
        IsBig = isBig;
        if(IsBig && _buttonBig != null) {
            _buttonBig.onClick.RemoveAllListeners();
            _buttonBig.onClick.AddListener(OnUpgradeButtonClicked);
            _buttonSmall.gameObject.SetActive(false);
            _buttonCurrent = _buttonBig;
            var r = _rectTransform.sizeDelta;
            r.x = 120f;
            _rectTransform.sizeDelta = r;
        } else if(!IsBig && _buttonSmall != null) {
            _buttonSmall.onClick.RemoveAllListeners();
            _buttonSmall.onClick.AddListener(OnUpgradeButtonClicked);
            _buttonCurrent = _buttonSmall;
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
    public void DisplayResources() {
        foreach (var item in upgradeData.requiredItems) {
            Instantiate(App.ResourceSystem.GetPrefab<UIUpgradeResourceDisplay>("UpgradeResourceDisplay"), _resourceContainer)
                .Init(item.item.icon,item.quantity);
        }
        _resourceContainer.gameObject.SetActive(true);
    }
    public void DestroyDisplayedResources() {
        foreach (Transform child in _resourceContainer)
        {
            Destroy(child.gameObject);
        }
    }
    public void OnPointerEnter(PointerEventData eventData) {
        OnPopupShow?.Invoke(this, true);
    }

    public void OnPointerExit(PointerEventData eventData) {
        OnPopupShow?.Invoke(this, false);
    }
    private void OnUpgradeButtonClicked() {
       // UICraftingManager.Instance.AttemptCraft(upgradeData, null, null);
    }
    // This method is called by the event from the UpgradeManager
    private void HandleUpgradePurchased(UpgradeRecipeSO purchasedRecipe) {
        // When any upgrade is purchased, re-evaluate our state.
        // This is important for unlocking nodes when a prerequisite is met.
        UpdateVisualState();
    }

    // The core logic for how this node should look based on game state
    private void UpdateVisualState() {
        if (upgradeData == null)
            return;

        bool isPurchased = UpgradeManager.Instance.IsUpgradePurchased(upgradeData);

        if (isPurchased) {
            // State: Purchased
            _buttonCurrent.interactable = false;
            // You might change the background color, show a checkmark, etc.
            GetComponent<Image>().color = Color.green;
        } else {
            bool prerequisitesMet = UpgradeManager.Instance.ArePrerequisitesMet(upgradeData);
            if (prerequisitesMet) {
                _buttonCurrent.interactable = true;
                //GetComponent<Image>().color = Color.yellow;
            } else {
                _buttonCurrent.interactable = false;
               // GetComponent<Image>().color = Color.gray;
            }
        }
    }
    public PopupData GetPopupData(InventoryManager clientInv) {
        return new PopupData(upgradeData.displayName, upgradeData.description, upgradeData.GetIngredientStatuses(clientInv));
        //return null;
    }
}
