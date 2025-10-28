﻿using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Should be generic enough to display any kind of data that popups up on the screen, either in world space, or on the canvas, for example next to the cursor 
public class UIPopup : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    [SerializeField] private RectTransform rectTransform;
    [SerializeField] private GameObject _iconContainer;
    [SerializeField] private Image _iconImage;
    [SerializeField] private Transform _statsChangeContainer;
    [SerializeField] private Transform _ingredientContainer;
    [SerializeField] private UIPopupUpgradeBar _upgradeBar;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public UIIngredientVisual ingredientPrefab;
    public ItemData itemData;
    private bool _isWorldPopup; // Is this popup on a world space canvas?
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
        
        // Crafting info and Status
        if (data.craftingInfo != null && data.craftingInfo.Count > 0) {
            foreach (Transform child in _ingredientContainer) {
                Destroy(child.gameObject);
            }
            foreach (var ingredient in data.craftingInfo) {
                Instantiate(ingredientPrefab, _ingredientContainer).Init(ingredient);
            }
            foreach (Transform child in _statsChangeContainer) {
                Destroy(child.gameObject);
            }
            foreach (var stat in data.statInfo) {
                //Todo obviously
                var statChange = Instantiate(App.ResourceSystem.GetPrefab<UIUpgradeStat>("UIUpgradeStatPopup"), _statsChangeContainer);
                statChange.Init(stat); // TODO
            }
        } 

        // Upgrade Status
        if(data.progressionInfo.ShouldShow) {
            // Set the status for the bar
            _upgradeBar.gameObject.SetActive(true);
            _upgradeBar.UpdateVisuals(data.progressionInfo);
        } else {
            _upgradeBar.gameObject.SetActive(false);
        }

        // Icon, used for control screen
        if (data.Icon != null) {
            _iconImage.sprite = data.Icon;
        } else if (_iconContainer != null) {
            _iconContainer.SetActive(false);
        }
    }
    public void HandleFailVisual() {
        if (_isWorldPopup) {
            transform.DOShakePosition(0.2f,0.3f,50);
        } else {
            transform.DOShakePosition(0.2f,15f,30);
        }
    }
}