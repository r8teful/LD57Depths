using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Color = UnityEngine.Color;

public class UIUpgradeNode : MonoBehaviour, IPopupInfo, IPointerEnterHandler, IPointerExitHandler {
    [SerializeField] private Button _buttonBig;
    [SerializeField] private Button _buttonSmall;
    [SerializeField] private TextMeshProUGUI _stageText;
    public ushort IDBoundNode = ResourceSystem.InvalidID; // Should match the NODE that its connected to 
    private Image _iconImage;
    private Button _buttonCurrent;
    private Image _imageCurrent;
    private CanvasGroup _canvasGroup;
    private UpgradeRecipeSO _preparedRecipeForPurchase;
    private InventoryManager _playerInventory;
    private RectTransform _rectTransform;
    private UpgradeNodeSO _boundNode;
    private UIUpgradeTree _treeParent;

    [OnValueChanged("InspectorBigChange")]
    public bool IsBig;
    public event Action PopupDataChanged;
    public event Action<UpgradeNodeState> OnStateChange;
    public enum UpgradeNodeState {Purchased,Purchable,Unlocked,Locked }

    private static readonly string ICON_PURCHASED_HEX = "#FFAA67";    
    private static readonly string ICON_AVAILABLE_HEX = "#FFFFFF";    
    private static readonly string ICON_NOT_AVAILABLE_HEX = "#9FB3B7"; 
    private static readonly string ICON_PRESSED_HEX = "#ECECEC";
    private static readonly string PARTICLE_PURCHASED = "#C41F66";      

    // 0 = Blue | Green | Orange
    // 1 = Active | Inactive | Pressed
    private const string SPRITE_PATTERN = "ButtonUpgrade{0}{1}";

    // cached Colors parsed from hex
    private Color _iconPurchasedColor;
    private Color _iconAvailableColor;
    private Color _iconNotAvailableColor;
    private Color _iconPressedColor;
    private Color _particlePurchasedColor;
    private UpgradeNodeState _cachedState;
    private int _cachedLevel;
    private bool _isSelected;

    private void Awake() {
        // parse hex colors (falls back to white if parse fails)
        ColorUtility.TryParseHtmlString(ICON_PURCHASED_HEX, out _iconPurchasedColor);
        ColorUtility.TryParseHtmlString(ICON_AVAILABLE_HEX, out _iconAvailableColor);
        ColorUtility.TryParseHtmlString(ICON_NOT_AVAILABLE_HEX, out _iconNotAvailableColor);
        ColorUtility.TryParseHtmlString(ICON_PRESSED_HEX, out _iconPressedColor);
        ColorUtility.TryParseHtmlString(PARTICLE_PURCHASED, out _particlePurchasedColor);
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
    internal void Init(UIUpgradeTree parent, InventoryManager inv, UpgradeNodeSO boundNode, int currentLevel, UpgradeRecipeSO preparedUpgrade, UpgradeNodeState status) {
        _treeParent = parent;
        _boundNode = boundNode;
        _preparedRecipeForPurchase = preparedUpgrade;
        _playerInventory = inv;
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 1;
        HandleButtonSize(); // Sets _buttonCurrent
        _imageCurrent = _buttonCurrent.targetGraphic.gameObject.GetComponent<Image>(); // omg so uggly
        _iconImage = _buttonCurrent.transform.GetChild(1).GetComponent<Image>();// Even worse
        SetIcon();
        UpdateVisual(status,currentLevel);
        //UpdateVisualState();
    }
    public void SetNewPreparedUpgrade(UpgradeRecipeSO prep) {
        _preparedRecipeForPurchase = prep;
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
    public void Select() {
        if (_preparedRecipeForPurchase == null) return;
        if (_cachedState == UpgradeNodeState.Locked) return;
        PopupManager.Instance.ShowPopup(this, true);
    }
    public void Deselect() {

        if (_preparedRecipeForPurchase == null) return;
        if (_cachedState == UpgradeNodeState.Locked) return;
        PopupManager.Instance.ShowPopup(this, false);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        Select();
    }

    public void OnPointerExit(PointerEventData eventData) {
        Deselect();
    }
    private void OnUpgradeButtonClicked() {
        // UICraftingManager.Instance.AttemptCraft(upgradeData, null, null);
        _treeParent.OnUpgradeButtonClicked(this,_boundNode);
    }
    
    public void UpdateVisual(UpgradeNodeState state, int currentLevel = -1) {
        // Derive the old boolean flags so we can still cache them if other code expects them.
        bool isPurchased = state == UpgradeNodeState.Purchased;
        bool prerequisitesMet = state == UpgradeNodeState.Unlocked;

        // Determine variant string (Orange for purchased, otherwise Green (IsBig) or Blue).
        string variant = isPurchased ? "Orange" : (IsBig ? "Green" : "Blue");
        _cachedLevel = currentLevel;
        if(_cachedState != state) {
            if (state == UpgradeNodeState.Purchased) {
                OnPurchased(); // Call it only once 
                OnPointerExit(null); // Closes popup, because we've purchased it we don't have anything to show!
            }
        }
        _cachedState = state;
        SetLevelText(currentLevel);
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
                _canvasGroup.alpha = 1; 
                _iconImage.color = _iconPurchasedColor;
                _buttonCurrent.interactable = false;
                OnStateChange?.Invoke(UpgradeNodeState.Purchased);
                break;

            case UpgradeNodeState.Unlocked:
                ApplySprite(variant, "Active");
                //SetLinesColour(_lineAvailableColor);
                _canvasGroup.alpha = 1; 
                _iconImage.color = _iconAvailableColor;
                _buttonCurrent.interactable = true;
                OnStateChange?.Invoke(UpgradeNodeState.Unlocked);
                //_treeParent.SetNodeAvailable(_upgradeData);
                break;

            case UpgradeNodeState.Locked:
            default:
                ApplySprite(variant, "Inactive"); 
                _canvasGroup.alpha = 0; // Just hide it for now, 
                //SetLinesColour(_lineNotAvailableColor);
                _iconImage.color = _iconNotAvailableColor;
                _buttonCurrent.interactable = false;
                OnStateChange?.Invoke(UpgradeNodeState.Locked);
                break;
        }

        PopupDataChanged?.Invoke(); //popup data could have changed
    }
    private void SetLevelText(int currentLevel) {
        if(_preparedRecipeForPurchase == null || _boundNode.MaxLevel <= 1)
            _stageText.gameObject.SetActive(false);
        _stageText.text = $"{currentLevel}/{_boundNode.MaxLevel}";
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
        return new PopupData(_boundNode.nodeName, upgradeData.description, 
            upgradeData.GetIngredientStatuses(clientInv),
            // We need a new way to get the stat statuses, it will depend on the upgrade. 
            statInfo: upgradeData.GetStatStatuses(), // This lagging behind, for some reason, rest is updating correctly
            progressionInfo: new(_boundNode.MaxLevel, _cachedLevel));
    }

    internal void SetSelected() {
        _isSelected = true;
    }

    internal void DoPurchaseAnim() {
        OnPurchased();
    }

    internal void OnPurchaseInput() {
        OnUpgradeButtonClicked();
        // todo
    }
}
