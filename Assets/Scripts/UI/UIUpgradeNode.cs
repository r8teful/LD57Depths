using FishNet.Object.Synchronizing;
using System;
using System.Drawing;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Color = UnityEngine.Color;

public class UIUpgradeNode : MonoBehaviour, IPopupInfo, IPointerEnterHandler, IPointerExitHandler {
    [SerializeField] private Button _buttonBig;
    [SerializeField] private Button _buttonSmall;
    private Image _iconImage;
    private Button _buttonCurrent;
    private Image _imageCurrent;
    private RectTransform _rectTransform;
    private UpgradeRecipeBase _upgradeData;
    private UIUpgradeTree _treeParent;

    // If we are networked we will subscribe to when an upgrade is purchased from the communal upgrade manager.
    // Also, when we purchase an upgrade we will notify the communal upgrade manager. 
    private bool _isNetworked; 
    
    public bool IsBig;
    public event Action PopupDataChanged;
    public event Action<IPopupInfo, bool> OnPopupShow;

    internal void Init(UpgradeRecipeBase upgradeRecipeSO, UIUpgradeTree parent, bool isBig, bool isNetworked) {
        _treeParent = parent;
        _upgradeData = upgradeRecipeSO;
        _rectTransform = GetComponent<RectTransform>();
        IsBig = isBig;
        _isNetworked = isNetworked;
        if (IsBig && _buttonBig != null) {
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
        _imageCurrent = _buttonCurrent.targetGraphic.gameObject.GetComponent<Image>(); // omg so uggly
        _iconImage = _buttonCurrent.transform.GetChild(1).GetComponent<Image>();// Even worse

        // Set icon
        var icon = App.ResourceSystem.GetSprite($"Upgrade{_upgradeData.type}");
        if(icon != null) {
            _iconImage.sprite = icon;
            _iconImage.SetNativeSize();
            Vector2 size = icon.rect.size *0.8f; // Just been doing 80% of the original size for the whole ui 
            _iconImage.rectTransform.sizeDelta = size;
            RectTransform rt = _iconImage.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);   // anchor at center
            rt.anchorMax = new Vector2(0.5f, 0.5f);   // anchor at center
            rt.pivot = new Vector2(0.5f, 0.5f);   // pivot at center
            rt.anchoredPosition = Vector2.zero;      // zero offset from anchor
            //_iconImage.rectTransform.sizeDelta = new Vector2(icon. texture.width, icon.texture.height);
        } else {
            Debug.LogError($"Icon for upgrade type {_upgradeData.type} not found!");
            return;
        }
        UpdateVisualState();
        SubscribeToEvents();
    }
    private void OnEnable() {
        
            UpgradeManagerPlayer.OnUpgradePurchased += HandleUpgradePurchased;
    }

    private void SubscribeToEvents() {
        if (_isNetworked) {
            UpgradeManagerCommunal.Instance.UnlockedCommunalUpgrades.OnChange += HandleCommunalUpgradePurchased;
        } else {
        }
    }

    private void HandleCommunalUpgradePurchased(SyncHashSetOperation op, ushort item, bool asServer) {
        if (op == SyncHashSetOperation.Add) {
            // When a communal upgrade is purchased, re-evaluate our state.
            UpdateVisualState();
        }
    }

    private void OnDisable() {
        if (_isNetworked) {
            UpgradeManagerCommunal.Instance.UnlockedCommunalUpgrades.OnChange -= HandleCommunalUpgradePurchased;
        } else {
            UpgradeManagerPlayer.OnUpgradePurchased -= HandleUpgradePurchased;
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
        UpgradeManagerPlayer.Instance.PurchaseUpgrade(_upgradeData);
    }
    // This method is called by the event from the UpgradeManager
    private void HandleUpgradePurchased(UpgradeRecipeBase purchasedRecipe) {
        // When any upgrade is purchased, re-evaluate our state.
        // This is important for unlocking nodes when a prerequisite is met.
        UpdateVisualState();
    }

    // The core logic for how this node should look based on game state
    private void UpdateVisualState() {
        if (_upgradeData == null)
            return;

        bool isPurchased = _isNetworked ? UpgradeManagerCommunal.Instance.IsUpgradePurchased(_upgradeData) : UpgradeManagerPlayer.Instance.IsUpgradePurchased(_upgradeData);

        if (isPurchased) {
            // State: Purchased
            var c = _buttonCurrent.colors;
            c.disabledColor = Color.white;
            _buttonCurrent.colors = c;
            _buttonCurrent.interactable = false;
            Color color;
            if (ColorUtility.TryParseHtmlString("#D58141", out color)) {
                _iconImage.color = color;
            }
            _imageCurrent.sprite = App.ResourceSystem.GetSprite($"UpgradeNode{(IsBig ? "Big" : "Small")}Purchased");
            //GetComponent<Image>().color = Color.green;
        } else {
            bool prerequisitesMet = UpgradeManagerPlayer.Instance.ArePrerequisitesMet(_upgradeData);
            if (prerequisitesMet) {
                // Available
                _buttonCurrent.interactable = true;
                _treeParent.SetNodeAvailable(_upgradeData);
                if (ColorUtility.TryParseHtmlString("#237C8A", out var color)) {
                    _iconImage.color = color;
                }
            } else {
                _buttonCurrent.interactable = false;
                if (ColorUtility.TryParseHtmlString("#124553", out var color)) {
                    _iconImage.color = color;
                }
                // GetComponent<Image>().color = Color.gray;
            }
        }
        // BIG INACTIVE 077263
        // BIG ACTIVE 0CD8BA

        // SMALL INACTIVE 124553
        // SMALL ACTIVE 237C8A
        // SMALL Purchad D58141
    }
    public PopupData GetPopupData(InventoryManager clientInv) {
        return new PopupData(_upgradeData.displayName, _upgradeData.description, _upgradeData.GetIngredientStatuses(clientInv));
        //return null;
    }
}
