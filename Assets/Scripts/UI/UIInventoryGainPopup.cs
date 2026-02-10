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
    private AudioSource _sound;
   // private float _sinceLastIncrease;
    private ushort _cachedID;

    //private bool _hasIncremented;
    private void Awake() {
        ItemTransferManager.OnItemTransferStop += ItemTransferStopped;
        PlayerLayerController.OnPlayerVisibilityChanged += PlayerLayerChange;
    }

    private void PlayerLayerChange(VisibilityLayerType type) {
        if(type == VisibilityLayerType.Exterior) {
            // stop audio if its playing 
            if (_sound != null && _sound.isPlaying) {
                _sound.DOFade(0, 0.5f).OnComplete(() => {
                    _sound.DOKill();
                    Destroy(_sound.gameObject);
                    _sound = null;
                });
            }
            OnDespawned?.Invoke(this);
            Destroy(gameObject);
        }
    }

    private void OnDestroy() {
        ItemTransferManager.OnItemTransferStop -= ItemTransferStopped;
        PlayerLayerController.OnPlayerVisibilityChanged -= PlayerLayerChange;
    }

    private void ItemTransferStopped(ushort id) {
        if (id == _cachedID && _sound != null) {
            _sound.DOFade(0, 0.5f).OnComplete(() => {
                _sound.DOKill();
                Destroy(_sound.gameObject);
                _sound = null;
            });
        }
    }

    
    public void Init(Sprite icon, int amount, ushort id) {
        if (canvasGroup == null) {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 1f;
        _cachedID = id;
        
        itemIconImage.sprite = icon;
        _quantity = amount;
        quantityText.text = _quantity.ToString();
        //Animation();
        ResetTimer();

        // Pop in animation
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        // sound
        _sound = AudioController.Instance.PlaySound2D("ItemAdd",0.1f,looping:true);
    }

    public void IncreaseAmount(int increaseBy) {
        _quantity += increaseBy;
        quantityText.text = _quantity.ToString();
        //Animation();
        ResetTimer();
        if(_sound != null)
            _sound.pitch = QuantityToPitch(_quantity);
        // If fading, cancel it
        if (isFading) {
            fadeTween?.Kill();
            canvasGroup.alpha = 1f;
            isFading = false;
        }
    }
    private void Animation() {
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

    float QuantityToPitch(long q) {
        if (q <= 0) return 1; // min pich is 1

        // normalized logarithmic value in [0,1]
        float tRaw = Mathf.Log(1f + q) / Mathf.Log(1f + 10000);
        tRaw = Mathf.Clamp01(tRaw);

        // curveExponent > 1 makes small quantities produce much smaller t
        float t = Mathf.Pow(tRaw, 3);

        return Mathf.Lerp(1, 4, t);
    }
}