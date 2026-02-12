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

    [SerializeField] private Sprite _unlockedSpriteSmall;
    [SerializeField] private Sprite _unlockedSpriteBig;
    [SerializeField] private Sprite _purchasableSpriteSmall;
    [SerializeField] private Sprite _purchasableSpriteBig;
    [SerializeField] private Sprite _purchasedSprite;

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
    public event Action<UpgradeNodeState,bool> OnStateChange; // bool is if it has been purshed atleast ONCE

    private static readonly string ICON_PURCHASED_HEX = "#FFAA67";    
    private static readonly string ICON_UNLOCKED_HEX = "#FFFFFF"; // slighly gray?
    private static readonly string ICON_PURCHASABLE_HEX = "#FFFFFF";

    private static readonly string PARTICLE_ORANGE = "#D58141";      
    private static readonly string PARTICLE_PURPLE = "#FF2986";      
    private static readonly string PARTICLE_PURPLELIGHT = "#860F75";      
    private static readonly string PARTICLE_PURPLEMEDIUM = "#C31F66";      

    // 0 = Blue | Green | Orange
    // 1 = Active | Inactive | Pressed
    private const string SPRITE_PATTERN = "ButtonUpgrade{0}{1}";

    // cached Colors parsed from hex
    private Color _iconPurchasedColor;
    private Color _iconPurchasableColor;
    private Color _iconUnlockedColor;
    private bool _isSelected;
    private Vector2 _preferedSize;

    private void Awake() {
        // parse hex colors (falls back to white if parse fails)
        ColorUtility.TryParseHtmlString(ICON_PURCHASED_HEX, out _iconPurchasedColor);
        ColorUtility.TryParseHtmlString(ICON_UNLOCKED_HEX, out _iconUnlockedColor);
        ColorUtility.TryParseHtmlString(ICON_PURCHASABLE_HEX, out _iconPurchasableColor);

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
    internal void Init(UIUpgradeTree parent, UpgradeNodeSO data, UpgradeManagerPlayer up) {
        _visualData = new(data, up);
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
            _preferedSize = r;
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
        if (_treeParent.IsClosing) return;
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
        //Debug.Log($" node: {gameObject.name} is updating its state to: {state}");
        SetLevelText();
        // If this button is currently selected, show the manual Pressed sprite and early return.
        if (_isSelected) {
            // Highlit it?? idk
        }
        SetSprite(state);
        switch (state) {
            case UpgradeNodeState.Purchased:
                _canvasGroup.alpha = 1;
                _iconImage.color = _iconPurchasedColor;
                _buttonCurrent.interactable = false;
                break;
            case UpgradeNodeState.Unlocked:
                _canvasGroup.alpha = 1;
                _iconImage.color = _iconUnlockedColor;
                _buttonCurrent.interactable = true;
                break;
            case UpgradeNodeState.Purchasable:
                _iconImage.color = _iconPurchasableColor;
                _buttonCurrent.interactable = true;
                _canvasGroup.alpha = 1;
                break;
            case UpgradeNodeState.Locked:
                _imageCurrent.sprite = null;
                _canvasGroup.alpha = 0;
                _buttonCurrent.interactable = false;
                break;
            default:
                
                break;
        }
        OnStateChange?.Invoke(state,_visualData.LevelCurrent>0);

    }

    private void SetSprite(UpgradeNodeState state) {
        _imageCurrent.sprite = state switch {
            UpgradeNodeState.Purchased => _purchasedSprite,

            UpgradeNodeState.Purchasable =>
                IsBig ? _purchasableSpriteBig : _purchasableSpriteSmall,

            UpgradeNodeState.Unlocked =>
                IsBig ? _unlockedSpriteBig : _unlockedSpriteSmall,
            _ => null
        };
    }

    private void SetLevelText() {
        if(_visualData.LevelMax<= 1)
            _stageText.gameObject.SetActive(false);
        _stageText.text = $"{_visualData.LevelCurrent}/{_visualData.LevelMax}";
    }
    private void OnPurchased() {
        // Hide popup
        App.AudioController.PlaySound2D("UpgradeBought");
        var p = App.ResourceSystem.GetPrefab("UIParticleUpgradePurchase");
        var m =p.GetComponentInChildren<ParticleSystem>().main;
        Color c;
        if (_visualData.IsMaxLevel()) {
            ColorUtility.TryParseHtmlString(PARTICLE_ORANGE, out c);
        } else {
            ColorUtility.TryParseHtmlString(PARTICLE_PURPLEMEDIUM, out c);
        }
        m.startColor = c;
        Instantiate(p, transform.position, Quaternion.identity, transform).transform.SetAsLastSibling();
        var vibrato = 5;
        var elasticity = 1;
        var scale = -0.1f;
        float rotation = 5;
        //_rectTransform.DOKill();
        _rectTransform.localScale = Vector3.one;
        _rectTransform.rotation = Quaternion.identity;

        _rectTransform.DOPunchScale(new(scale, scale, scale), 0.2f, vibrato, elasticity)
            .OnComplete(() => {
                _rectTransform.localScale = Vector3.one;
                _rectTransform.rotation = Quaternion.identity;
                });
        _rectTransform.DOPunchRotation(new(0, 0, UnityEngine.Random.Range(-rotation, rotation)), 0.2f, vibrato, elasticity)
            .OnComplete(() => {
                _rectTransform.localScale = Vector3.one;
                _rectTransform.rotation = Quaternion.identity;
            });
    }

    public PopupData GetPopupData(InventoryManager clientInv) {
        // Stat data
        _visualData.UpdateForPopup();
        return new PopupData(_visualData.Title, _visualData.Description,
            _visualData.IngredientStatuses, // We'll have to pull this everytime we want to show it because 
                                            // We need a new way to get the stat statuses, it will depend on the upgrade. 
            statInfo: _visualData.StatChangeStatuses, 
            progressionInfo: new(_visualData.LevelMax, _visualData.LevelCurrent),
            icon: _visualData.IconExtra
            );
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

    internal void OnUpgraded() {
        // update visual data
        _visualData.UpdateForUpgradePurchase();
        OnPurchased();
        PopupDataChanged?.Invoke(); // This will tell the upgrade manager to fetch new upgrade data
        _treeParent.UpdateNodeVisualData();
        UpdateVisual(); // Sets color, stage text etc...
    }

    // This is for when inderect nodes need to update their visualdata when a prerequaized 
    internal void UpdateVisualData() {
        _visualData.UpdateForUpgradePurchase();
        UpdateVisual(); 
    }

    internal void DoPulseAnim(int depth) {
        float maxDepth = 6;
        var depthProgress = Mathf.Clamp01( depth / maxDepth);
        // sine ease (from easings.net
        //float depthRatio = (float)((float)1 - ((1 - Math.Cos(depthProgress * Math.PI)) / 2));
        float depthRatio = Mathf.Pow(1-depthProgress,3);
        //Debug.Log($"depth: {depth} gives progress: {depthProgress} gives ratio {depthRatio}");
        int vibrato = (int)(5 * depthRatio);
        float elasticity = 1 * depthRatio;
        float scale = -0.2f *depthRatio;
        float rotation = 5;
        _rectTransform.DOPunchScale(new(scale, scale, scale), 0.2f, vibrato, elasticity)
            .OnComplete(() => {
                _rectTransform.localScale = Vector3.one;
                _rectTransform.rotation = Quaternion.identity;
            });
        _rectTransform.DOPunchRotation(new(0, 0, UnityEngine.Random.Range(-rotation, rotation)), 0.2f, vibrato, elasticity)
            .OnComplete(() => {
                _rectTransform.localScale = Vector3.one;
                _rectTransform.rotation = Quaternion.identity;
            });
    }
}