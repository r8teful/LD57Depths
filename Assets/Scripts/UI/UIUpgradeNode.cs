using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using Color = UnityEngine.Color;

public class UIUpgradeNode : MonoBehaviour, IPopupInfo, IPointerEnterHandler, IPointerExitHandler {
    [SerializeField] private Button _buttonBig;
    [SerializeField] private Button _buttonSmall;
    public ushort IDBoundNode = ResourceSystem.InvalidID; // Should match the NODE that its connected to 
    private Image _iconImage;
    private Button _buttonCurrent;
    private Image _imageCurrent;
    private UpgradeRecipeSO _preparedRecipeForPurchase;
    private RectTransform _rectTransform;
    private UpgradeNodeSO _boundNode;
    private UIUpgradeTree _treeParent;

    [OnValueChanged("InspectorBigChange")]
    public bool IsBig;
    public event Action PopupDataChanged;
    public event Action<UpgradeNodeState> OnStateChange;
    public enum UpgradeNodeState {Purchased,Active,Inactive }

    private static readonly string ICON_PURCHASED_HEX = "#FFAA67";     // icon on purchased (orange) background
    private static readonly string ICON_AVAILABLE_HEX = "#FFFFFF";     // icon when available (active)
    private static readonly string ICON_NOT_AVAILABLE_HEX = "#9FB3B7";  // icon when unavailable (inactive / dim)
    private static readonly string ICON_PRESSED_HEX = "#ECECEC";       // icon when pressed (slightly different)  

    // 0 = Blue | Green | Orange
    // 1 = Active | Inactive | Pressed
    private const string SPRITE_PATTERN = "ButtonUpgrade{0}{1}";

    // cached Colors parsed from hex
    private Color _iconPurchasedColor;
    private Color _iconAvailableColor;
    private Color _iconNotAvailableColor;
    private Color _iconPressedColor;
    private UpgradeNodeState _cachedState;
    private bool _isSelected;

    private void Awake() {
        // parse hex colors (falls back to white if parse fails)
        ColorUtility.TryParseHtmlString(ICON_PURCHASED_HEX, out _iconPurchasedColor);
        ColorUtility.TryParseHtmlString(ICON_AVAILABLE_HEX, out _iconAvailableColor);
        ColorUtility.TryParseHtmlString(ICON_NOT_AVAILABLE_HEX, out _iconNotAvailableColor);
        ColorUtility.TryParseHtmlString(ICON_PRESSED_HEX, out _iconPressedColor);
    }
    public void InspectorBigChange() {
        if (IsBig) {
            _buttonBig.gameObject.SetActive(true);
            _buttonSmall.gameObject.SetActive(false);
        } else {
            _buttonBig.gameObject.SetActive(false);
            _buttonSmall.gameObject.SetActive(true);
        }
    }
    internal void Init(UIUpgradeTree parent, UpgradeNodeSO boundNode, int currentLevel, UpgradeRecipeSO preparedUpgrade, UpgradeNodeState status) {
        _treeParent = parent;
        _boundNode = boundNode;
        _preparedRecipeForPurchase = preparedUpgrade;
        _rectTransform = GetComponent<RectTransform>();
        HandleButtonSize(); // Sets _buttonCurrent
        _imageCurrent = _buttonCurrent.targetGraphic.gameObject.GetComponent<Image>(); // omg so uggly
        _iconImage = _buttonCurrent.transform.GetChild(1).GetComponent<Image>();// Even worse
        SetIcon();
        UpdateVisual(status);
        //UpdateVisualState();
    }

    private void SetIcon() {
        var icon = _boundNode.icon;
        if (icon != null) {
            _iconImage.sprite = icon;
            var c = _iconImage.color;
            c.a = 1;
            _iconImage.color = c; // Make sure alpha is 1
            _iconImage.SetNativeSize();
            Vector2 size = icon.rect.size * 0.8f; // Just been doing 80% of the original size for the whole ui 
            _iconImage.rectTransform.sizeDelta = size;
            RectTransform rt = _iconImage.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);   // anchor at center
            rt.anchorMax = new Vector2(0.5f, 0.5f);   // anchor at center
            rt.pivot = new Vector2(0.5f, 0.5f);   // pivot at center
            rt.anchoredPosition = Vector2.zero;      // zero offset from anchor
            //_iconImage.rectTransform.sizeDelta = new Vector2(icon. texture.width, icon.texture.height);
        } else {
            Debug.LogError($"Icon for upgrade type {_boundNode.name} not found!");
        }
    }

    private void HandleButtonSize() {
        if (IsBig && _buttonBig != null) {
            _buttonBig.onClick.RemoveAllListeners();
            _buttonBig.onClick.AddListener(OnUpgradeButtonClicked);
            _buttonSmall.gameObject.SetActive(false);
            _buttonCurrent = _buttonBig;
            var r = _rectTransform.sizeDelta;
            //r.x = 120f;
            _rectTransform.sizeDelta = r;
        } else if (!IsBig && _buttonSmall != null) {
            _buttonSmall.onClick.RemoveAllListeners();
            _buttonSmall.onClick.AddListener(OnUpgradeButtonClicked);
            _buttonCurrent = _buttonSmall;
            _buttonBig.gameObject.SetActive(false);
            var r = _rectTransform.sizeDelta;
            //r.x = 65f;
            _rectTransform.sizeDelta = r;
        }
    }


    public void OnPointerEnter(PointerEventData eventData) {
        if (_preparedRecipeForPurchase == null) return;
        if (_cachedState == UpgradeNodeState.Purchased) return;
        PopupManager.Instance.ShowPopup(this, true);
    }

    public void OnPointerExit(PointerEventData eventData) {
        if (_preparedRecipeForPurchase == null) return;
        if (_cachedState == UpgradeNodeState.Purchased) return;
        PopupManager.Instance.ShowPopup(this, false);
    }
    private void OnUpgradeButtonClicked() {
        // UICraftingManager.Instance.AttemptCraft(upgradeData, null, null);
        _treeParent.OnUpgradeButtonClicked(_boundNode);
    }
    
    // The core logic for how this node should look based on game state
    private void UpdateVisualState() {
        //UpdateVisual();
        PopupDataChanged?.Invoke(); // Data could have changed
        // BIG INACTIVE 077263
        // BIG ACTIVE 0CD8BA

        // SMALL INACTIVE 124553
        // SMALL ACTIVE 237C8A
        // SMALL Purchad D58141
    }
    public void UpdateVisual(UpgradeNodeState state) {
        // Derive the old boolean flags so we can still cache them if other code expects them.
        bool isPurchased = state == UpgradeNodeState.Purchased;
        bool prerequisitesMet = state == UpgradeNodeState.Active;

        // Determine variant string (Orange for purchased, otherwise Green (IsBig) or Blue).
        string variant = isPurchased ? "Orange" : (IsBig ? "Green" : "Blue");
        if(_cachedState != state) {
            if (state == UpgradeNodeState.Purchased)
                OnPurchased(); // Call it only once 
        }
        _cachedState = state; 

        // If this button is currently selected, show the manual Pressed sprite and early return.
        if (_isSelected) {
            // Manual pressed state (we don't use Button's Sprite Swap)
            ApplySprite(variant, "Pressed");

            // Use pressed icon color (keeps previous logic)
            _iconImage.color = _iconPressedColor;

            // Interactable remains what the base state dictates (optional).
            // For purchased -> not interactable; for active -> interactable; for inactive -> not.
            _buttonCurrent.interactable = !isPurchased && prerequisitesMet;

            // Still report the state change for listeners (selected pressed still represents underlying state).
            OnStateChange?.Invoke(state);
            return;
        }

        // Not selected -> show base (Purchased / Active / Inactive)
        switch (state) {
            case UpgradeNodeState.Purchased:
                ApplySprite("Orange", "Inactive"); // purchased shows Orange Inactive
                                                   //SetLinesColour(_linePurchasedColor);
                _iconImage.color = _iconPurchasedColor;
                _buttonCurrent.interactable = false;
                OnStateChange?.Invoke(UpgradeNodeState.Purchased);
                break;

            case UpgradeNodeState.Active:
                ApplySprite(variant, "Active");
                //SetLinesColour(_lineAvailableColor);
                _iconImage.color = _iconAvailableColor;
                _buttonCurrent.interactable = true;
                OnStateChange?.Invoke(UpgradeNodeState.Active);
                //_treeParent.SetNodeAvailable(_upgradeData);
                break;

            case UpgradeNodeState.Inactive:
            default:
                ApplySprite(variant, "Inactive");
                //SetLinesColour(_lineNotAvailableColor);
                _iconImage.color = _iconNotAvailableColor;
                _buttonCurrent.interactable = false;
                OnStateChange?.Invoke(UpgradeNodeState.Inactive);
                break;
        }
    }
    private void ApplySprite(string variant, string state) {
        if (_imageCurrent == null) return;

        string spriteName = string.Format(SPRITE_PATTERN, variant, state);
        var sprite = App.ResourceSystem.GetSprite(spriteName);
        if (sprite == null) {
            Debug.LogError("Could not find valid sprite!");
            return;
        }
        _imageCurrent.sprite = sprite;
    }
    private void OnPurchased() {
        // Hide popup
        OnPointerExit(new(null));
        App.AudioController.PlaySound2D("UpgradeBought");
        var p = App.ResourceSystem.GetPrefab("UIParticleUpgradePurchase");
        Instantiate(p, transform.position, Quaternion.identity, transform);
        var vibrato = 5;
        var elasticity = 1;
        var scale = -0.1f;
        transform.DOPunchScale(new(scale, scale, scale), 0.2f, vibrato, elasticity);
        transform.DOPunchRotation(new(0, 0, UnityEngine.Random.Range(-2f, 2f)), 0.2f, vibrato, elasticity);
    }
    private void ConfigureColorBlockForState(Color pressedColor, Color disabledColor) {
        if (_buttonCurrent == null) return;

        var cb = _buttonCurrent.colors;
        // keep the normal/highlight colors default but ensure pressed/disabled are set
        cb.pressedColor = pressedColor;
        cb.disabledColor = disabledColor;
        cb.fadeDuration = 0;
        _buttonCurrent.colors = cb;
    }
    public PopupData GetPopupData(InventoryManager clientInv) {
        // First get the upgrade we are displaying
        UpgradeRecipeSO upgradeData = _preparedRecipeForPurchase;

        // Stat data
        return new PopupData(upgradeData.displayName, upgradeData.description, 
            upgradeData.GetIngredientStatuses(clientInv),
            statInfo: upgradeData.GetStatStatuses());
    }

    internal void SetSelected() {
        _isSelected = true;
    }
}
