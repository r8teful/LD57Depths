using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UITabButton : MonoBehaviour {
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private RectTransform _rectTransform;
    private float anchoredX;
    private void Awake() {
        anchoredX = _rectTransform.anchoredPosition.x;
    }
    public void SetButtonVisual(bool setActive) {
        if (setActive) {
            _backgroundImage.sprite = App.ResourceSystem.GetSprite("InventoryTabButtonActive");
            _rectTransform.DOAnchorPosX(30, 0.3f);
        } else {
            _backgroundImage.sprite = App.ResourceSystem.GetSprite("InventoryTabButtonInactive");
            _rectTransform.DOAnchorPosX(0, 0.3f);
        }
        
    }
}