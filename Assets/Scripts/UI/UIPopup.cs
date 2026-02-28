using Coffee.UIExtensions;
using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using Image = UnityEngine.UI.Image;

// Should be generic enough to display any kind of data that popups up on the screen, either in world space, or on the canvas, for example next to the cursor 
public class UIPopup : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private GameObject _iconContainer;
    [SerializeField] private Image _iconImage;
    [SerializeField] private Image _descriptionDivider;
    [SerializeField] private Transform _statsChangeContainer;
    [SerializeField] private Transform _ingredientContainer;
    [SerializeField] private UIPopupUpgradeBar _upgradeBar;
    [SerializeField] private UIParticle _particlesPurchaseUp;
    [SerializeField] private UIParticle _particlesPurchaseDown;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI _stageText;
    public UIIngredientVisual ingredientPrefab;
    public ItemData itemData;
    private bool _isWorldPopup; // Is this popup on a world space canvas?
    private void Awake() {
        UIUpgradeTree.OnUpgradeButtonPurchased += UpgradePurchased;
    }
    private void OnDisable() {
        UIUpgradeTree.OnUpgradeButtonPurchased -= UpgradePurchased;
    }

    private void UpgradePurchased() {
        int vibrato = 8;
        var elasticity = 2;
        var scale = 0.6f;
        float rotation =15;
        float time = 0.6f;
        //_particlesPurchaseUp.StartEmission();
        _particlesPurchaseUp.Play();
//        _particlesPurchaseDown.StartEmission();
        _particlesPurchaseDown.Play();
        //float strength = 10f;
        rectTransform.DOPunchScale(new(scale, 0, 0), time*0.5f, vibrato, elasticity)
            .SetEase(Ease.OutElastic);
            //..SetEase(Ease.OutSine);
        rectTransform.DOPunchRotation(new(0, 0, Random.value > 0.5 ? -rotation :  rotation), time, vibrato, elasticity)
            //.SetEase(Ease.OutBack);
            .SetEase(Ease.OutElastic);
        //rectTransform.DOPunchRotation(new(0, 0, Random.value > 0.5 ? -rotation :  rotation), 0.2f)
        //  .SetEase(Ease.OutElastic);
    }

    private void Start() {
        // Try and find the canvas lol
        Canvas c0 = null;
        Canvas c1 = null;
        c0 = GetComponentInParent<Canvas>();
        var t = transform.parent;
        if (t != null) {
            c1 = t.GetComponentInParent<Canvas>();
        }
        if (c0 != null) {
            _isWorldPopup = c0.renderMode == RenderMode.WorldSpace;
        }  else if (c1 != null){
            _isWorldPopup = c0.renderMode == RenderMode.WorldSpace;
        }
    }
    public void OnPointerEnter(PointerEventData eventData) {
       // PopupManager.Instance.OnPointerEnterPopup();
    }

    public void OnPointerExit(PointerEventData eventData) {
        //PopupManager.Instance.OnPointerExitPopup();
    }
    public void SetData(PopupData data) {
        // Name and description
        nameText.text = data.title;
        descriptionText.text = data.description;
        if(data.description == string.Empty) {
            _descriptionDivider.gameObject.SetActive(false);
        }
        // Destroy old
        foreach (Transform child in _ingredientContainer) {
            Destroy(child.gameObject);
        }
        foreach (Transform child in _statsChangeContainer) {
            Destroy(child.gameObject);
        }
        // Crafting info and Status
        if (data.craftingInfo != null && data.craftingInfo.Count > 0) {
            foreach (var ingredient in data.craftingInfo) {
                Instantiate(ingredientPrefab, _ingredientContainer).Init(ingredient);
            }
        }
        if(data.upgradeEffects != null &&  data.upgradeEffects.Count > 0) {
            bool treatHeaderAsStatText = false;
            if (data.upgradeEffects.Count == 1) {
                // If only one stat, treat header as stat text
                treatHeaderAsStatText = true;
            }
            foreach (var stat in data.upgradeEffects) {
                if(stat == null) continue;
                var statChange = Instantiate(App.ResourceSystem.GetPrefab<UIUpgradeStat>("UIUpgradeStatPopup"), _statsChangeContainer);
                statChange.Init(stat, treatHeaderAsStatText); 
            }
        }

        // Upgrade progression status
        if(data.progressionInfo.ShouldShow) {
            // Set the status for the bar
            _upgradeBar.gameObject.SetActive(true);
            _upgradeBar.UpdateVisuals(data.progressionInfo);
            _stageText.gameObject.SetActive(true);
            _stageText.text = $"{data.progressionInfo.LevelCurr}/{data.progressionInfo.LevelMax}";
        } else {
            _stageText.gameObject.SetActive(false);
            _upgradeBar.gameObject.SetActive(false);
        }

        // Icon, used for control screen
        if (data.Icon != null) {
            _iconImage.sprite = data.Icon;
            SetRightImageHeightNextFrame();

        } else if (_iconContainer != null) {
            _iconContainer.SetActive(false);
        }
    }

    private void SetRightImageHeightNextFrame() {
        StartCoroutine(SetHeightNextFrame());
    }
    private IEnumerator SetHeightNextFrame() {
        yield return null;

        const float originalW = 39f;
        const float originalH = 65f;
        float aspect = originalW / originalH;
        float targetHeight = rectTransform.rect.height;
        float newWidth = targetHeight * aspect;

        // Respect RectTransform layout (works with different anchors)
        _iconImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        _iconImage.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);

        _iconContainer.SetActive(true);// Enable 
    }

    public void HandleFailVisual() {
        if (_isWorldPopup) {
            transform.DOShakePosition(0.2f,0.3f,50);
        } else {
            transform.DOShakePosition(0.2f,15f,30);
        }
    }
    private void OnDestroy() {
        transform.DOKill();
    }

    public void ShowAnimate() {
        transform.localScale = Vector2.one * 0.5f;
        transform.DOScale(1,0.1f);
        transform.DOShakeRotation(0.2f, 6f, 50);
        
    }
}