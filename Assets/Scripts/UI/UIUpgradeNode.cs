using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;
using Color = UnityEngine.Color;

public class UIUpgradeNode : MonoBehaviour, IPopupInfo, IPointerEnterHandler, IPointerExitHandler {
    [SerializeField] private Button _buttonBig;
    [SerializeField] private Button _buttonSmall;
    public string GUIDBoundNode; // Should match the NODE that its connected to 
    private Image _iconImage;
    private Button _buttonCurrent;
    private Image _imageCurrent;
    private RectTransform _rectTransform;
    private UpgradeNode _boundNode;
    private UIUpgradeTree _treeParent;
    private UpgradeRecipeSO _baseRecipeForInfo; // The raw SO for displaying icon/description
    private UpgradeRecipeSO _preparedRecipeForPurchase; // The temporary instance with calculated cost

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
    private bool _cachedIsPurchased;
    private bool _cachedPrerequisitesMet;
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
    internal void Init(UIUpgradeTree parent, UpgradeNode boundNode, int currentLevel, UpgradeRecipeSO baseRecipeForInfo, UpgradeRecipeSO preparedNextStage, UpgradeNodeState status) {
        _treeParent = parent;
        _boundNode = boundNode;
        _baseRecipeForInfo = baseRecipeForInfo;
        _preparedRecipeForPurchase = preparedNextStage;
        _rectTransform = GetComponent<RectTransform>();
        _imageCurrent = _buttonCurrent.targetGraphic.gameObject.GetComponent<Image>(); // omg so uggly
        _iconImage = _buttonCurrent.transform.GetChild(1).GetComponent<Image>();// Even worse
        HandleButtonSize();
        SetIcon();
        UpdateVisualState();
    }

    private void SetIcon() {
        var icon = _baseRecipeForInfo.icon;
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
            Debug.LogError($"Icon for upgrade type {_baseRecipeForInfo.name} not found!");
        }
    }

    private void HandleButtonSize() {
        if (IsBig && _buttonBig != null) {
            _buttonBig.onClick.RemoveAllListeners();
            _buttonBig.onClick.AddListener(OnUpgradeButtonClicked);
            _buttonSmall.gameObject.SetActive(false);
            _buttonCurrent = _buttonBig;
            var r = _rectTransform.sizeDelta;
            r.x = 120f;
            _rectTransform.sizeDelta = r;
        } else if (!IsBig && _buttonSmall != null) {
            _buttonSmall.onClick.RemoveAllListeners();
            _buttonSmall.onClick.AddListener(OnUpgradeButtonClicked);
            _buttonCurrent = _buttonSmall;
            _buttonBig.gameObject.SetActive(false);
            var r = _rectTransform.sizeDelta;
            r.x = 65f;
            _rectTransform.sizeDelta = r;
        }
    }


    public void OnPointerEnter(PointerEventData eventData) {
        //PopupManager.Instance.ShowPopup(this, true);
    }

    public void OnPointerExit(PointerEventData eventData) {
        //PopupManager.Instance.ShowPopup(this, false);
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

    public void UpdateVisual(bool isPurchased,bool prerequisitesMet) {
        //var isPurchased = UpgradeManagerPlayer.LocalInstance.IsUpgradePurchased(_upgradeData);
        // determine variant (one of Blue/Green/Orange)
        string variant = isPurchased ? "Orange" : (IsBig ? "Green" : "Blue");
        //bool prerequisitesMet = UpgradeManagerPlayer.LocalInstance.ArePrerequisitesMet(_upgradeData);
        // Cache for later restore (when selection changes away)
        _cachedIsPurchased = isPurchased;
        _cachedPrerequisitesMet = prerequisitesMet;
        // If this button is the currently selected one, show the manual Pressed sprite:

        if (_isSelected) {
            // Manual pressed state (we don't use Button's Sprite Swap)
            ApplySprite(variant, "Pressed");
            _iconImage.color = _iconPressedColor;
            // Interactable remains what the base state dictates (optional)
            _buttonCurrent.interactable = !isPurchased && prerequisitesMet;
            return;
        }

        // Not the selected button -> show base (Purchased / Active / Inactive)
        if (isPurchased) {
            ApplySprite("Orange", "Inactive");
            //SetLinesColour(_linePurchasedColor);
            _iconImage.color = _iconPurchasedColor;
            _buttonCurrent.interactable = false;
            OnStateChange?.Invoke(UpgradeNodeState.Purchased);
        } else if (prerequisitesMet) {
            ApplySprite(variant, "Active");
            OnStateChange?.Invoke(UpgradeNodeState.Active);
           // SetLinesColour(_lineAvailableColor);
            _iconImage.color = _iconAvailableColor;
            _buttonCurrent.interactable = true;

            //_treeParent.SetNodeAvailable(_upgradeData);
        } else {
            ApplySprite(variant, "Inactive");
            //SetLinesColour(_lineNotAvailableColor);
            _iconImage.color = _iconNotAvailableColor;
            _buttonCurrent.interactable = false;
            OnStateChange?.Invoke(UpgradeNodeState.Inactive);
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
        //return new PopupData(_upgradeData.displayName, _upgradeData.description, _upgradeData.GetIngredientStatuses(clientInv));
        return null; // tODO
    }

    internal void SetSelected() {
        _isSelected = true;
    }
}
