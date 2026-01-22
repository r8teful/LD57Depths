using DG.Tweening;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
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
    private RectTransform _rectTransform;
    public RectTransform Rect => _rectTransform;
    private UIUpgradeTree _treeParent;
    private UpgradeNodeVisualData _visualData;
    public UpgradeNodeState GetState => _visualData.State;
    public UpgradeNodeVisualData GetVisualData => _visualData;
    [OnValueChanged("InspectorBigChange")]
    public bool IsBig;
    public event Action PopupDataChanged;
    public event Action<UpgradeNodeState> OnStateChange;

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
    private bool _isSelected;

    private void Awake() {
        // parse hex colors (falls back to white if parse fails)
        ColorUtility.TryParseHtmlString(ICON_PURCHASED_HEX, out _iconPurchasedColor);
        ColorUtility.TryParseHtmlString(ICON_AVAILABLE_HEX, out _iconAvailableColor);
        ColorUtility.TryParseHtmlString(ICON_NOT_AVAILABLE_HEX, out _iconNotAvailableColor);
        ColorUtility.TryParseHtmlString(ICON_PRESSED_HEX, out _iconPressedColor);
        ColorUtility.TryParseHtmlString(PARTICLE_PURCHASED, out _particlePurchasedColor);

        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 1;
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
    internal void Init(UIUpgradeTree parent,UpgradeTreeDataSO treeData,
        UpgradeNodeSO data,InventoryManager inv, HashSet<ushort> existingUpgrades) {
        _visualData = new(data,inv, treeData,existingUpgrades);
        _treeParent = parent;
        HandleButtonSize(); // Sets _buttonCurrent
        SetIcon();
        UpdateVisual();
    }

    private void SetIcon() {
        var icon = _visualData.Icon;
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
            Debug.LogError($"Icon for upgrade type {_visualData.Title} not found!");
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
        _imageCurrent = _buttonCurrent.targetGraphic.gameObject.GetComponent<Image>(); // omg so uggly
        _iconImage = _buttonCurrent.transform.GetChild(1).GetComponent<Image>();// Even worse
    }
    public void Select(bool usingPointer) {
        if (_visualData.State == UpgradeNodeState.Locked) return;
        if (usingPointer) {
            // No coroutine movement, simply show the popup
            PopupManager.Instance.ShowPopup(this, true);
        } else {
            StartCoroutine(SelectRoutine());
        }
    }
    private IEnumerator SelectRoutine() {
        // Simple solution, wait untill we've gotten to the target, and then we posision popup
        yield return _treeParent.OnPanSelect(this);
        PopupManager.Instance.ShowPopup(this, true);
    }
    public void Deselect() {
        if (_visualData.State == UpgradeNodeState.Locked) return;
        PopupManager.Instance.ShowPopup(this, false);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        Select(usingPointer: true);
    }

    public void OnPointerExit(PointerEventData eventData) {
        Deselect();
    }
    private void OnUpgradeButtonClicked() {
        if (_visualData.Node.stages.Count == 0) return; // Some nodes have any stages and it will give null
        _treeParent.OnUpgradeButtonClicked(this,_visualData.Node); // This seems wrong but its where we store what actual node we are
    }
    
    public void UpdateVisual() {
        var state = _visualData.State;
        // Derive the old boolean flags so we can still cache them if other code expects them.
        bool isPurchased = state == UpgradeNodeState.Purchased;
        bool prerequisitesMet = state == UpgradeNodeState.Unlocked;

        // Determine variant string (Orange for purchased, otherwise Green (IsBig) or Blue).
        string variant = isPurchased ? "Orange" : (IsBig ? "Green" : "Blue");
        SetLevelText();
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
    
    }
    private void SetLevelText() {
        if(_visualData.LevelMax<= 1)
            _stageText.gameObject.SetActive(false);
        _stageText.text = $"{_visualData.LevelCurrent}/{_visualData.LevelMax}";
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
        transform.DOKill();
        transform.DOPunchScale(new(scale, scale, scale), 0.2f, vibrato, elasticity);
        transform.DOPunchRotation(new(0, 0, UnityEngine.Random.Range(-2f, 2f)), 0.2f, vibrato, elasticity);
    }

    public PopupData GetPopupData(InventoryManager clientInv) {
        // Stat data
        _visualData.UpdateForPopup(clientInv);
        return new PopupData(_visualData.Title, _visualData.Description, 
            _visualData.IngredientStatuses, // We'll have to pull this everytime we want to show it because 
            // We need a new way to get the stat statuses, it will depend on the upgrade. 
            statInfo: _visualData.StatChangeStatuses, // This lagging behind, for some reason, rest is updating correctly
            progressionInfo: new(_visualData.LevelMax, _visualData.LevelCurrent));
    }

    internal void SetSelected() {
        _isSelected = true;
    }

    internal void DoPurchaseAnim() {
        OnPurchased();
    }

    internal void OnPurchaseInput() {
        OnUpgradeButtonClicked();
    }

    internal void OnUpgraded(HashSet<ushort> unlockedUpgrades) {
        // update visual data
        _visualData.UpdateForUpgradePurchase(unlockedUpgrades);
        OnPurchased();
        // Close popup if we've reached max level
        if (_visualData.IsMaxLevel()) {
            OnPointerExit(null); // Closes popup, because we've purchased it we don't have anything to show!
            // We need to tell whatever nodes have this one as requirement to update their state now
            _treeParent.UpdateConnectedNodes(_visualData.Node);
        } else {
            PopupDataChanged.Invoke(); // This will tell the upgrade manager to fetch new upgrade data
        }
        UpdateVisual(); // Sets color, stage text etc...
    }

    // This is for when inderect nodes need to update their visualdata when a prerequaized 
    internal void UpdateVisualData(HashSet<ushort> unlockedUpgrades) {
        _visualData.UpdateForUpgradePurchase(unlockedUpgrades);
        UpdateVisual(); 
    }
}