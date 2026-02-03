using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIInventoryGainPopup : MonoBehaviour {
    [SerializeField] private Image itemIconImage;
    [SerializeField] private TextMeshProUGUI quantityText;
    [SerializeField] private CanvasGroup canvasGroup;

    private int _quantity;
    public Action<UIInventoryGainPopup> OnDespawned { get; internal set; }

    private float lastUpdateTime;
    private const float DespawnDelay = 1f;
    private bool isFading = false;
    private Tween fadeTween;

    private void OnDestroy() {
        transform.DOKill();
    }
    public void Init(Sprite icon, int amount) {
        if (canvasGroup == null) {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 1f;

        itemIconImage.sprite = icon;
        _quantity = amount;
        UpdateCount();
        ResetTimer();

        // Pop in animation
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
    }

    public void IncreaseAmount(int increaseBy) {
        _quantity += increaseBy;
        UpdateCount();
        ResetTimer();

        // If fading, cancel it
        if (isFading) {
            fadeTween?.Kill();
            canvasGroup.alpha = 1f;
            isFading = false;
        }
    }

    private void UpdateCount() {
        quantityText.text = _quantity.ToString();
        quantityText.transform.localScale = Vector3.one;
        quantityText.transform.DOKill();
        var punchAmount = 1.0001f;
        quantityText.transform.DOPunchScale(new Vector3(punchAmount, punchAmount, punchAmount), 0.3f, 1, 1f);
    }

    private void ResetTimer() {
        lastUpdateTime = Time.time;
    }

    private void Update() {
        if (!isFading && Time.time - lastUpdateTime > DespawnDelay) {
            isFading = true;
            fadeTween = canvasGroup.DOFade(0f, 1f).OnComplete(() => {
                OnDespawned?.Invoke(this);
                Destroy(gameObject);
            });
        }
    }
}