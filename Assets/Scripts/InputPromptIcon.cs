using DG.Tweening;
using InputSystemActionPrompts;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InputPromptIcon : MonoBehaviour {
    [SerializeField] private Image _interactIconImage;
    [SerializeField] private Image _interactPromptImage;
    [SerializeField] private TextMeshProUGUI _interactIconText;
    private CanvasGroup _canvasGroup;
    private RectTransform _rect;

    private void Awake() {
        _canvasGroup = GetComponent<CanvasGroup>();
        _rect = GetComponent<RectTransform>();
        if(_canvasGroup == null || _rect ==null) {
            Debug.LogError("Could not find component!");
        }
        InputDevicePromptSystem.OnActiveDeviceChanged += DeviceChange;
        _canvasGroup.alpha = 0;
        //_rect.anchoredPosition = Vector2.zero;
        UpdateString();
    }

    

    private void OnDestroy() {
        _rect.DOKill();
        _canvasGroup.DOKill();
        InputDevicePromptSystem.OnActiveDeviceChanged -= DeviceChange;
    }
    private void DeviceChange(UnityEngine.InputSystem.InputDevice device) {
        UpdateString();
    }

    public void Init(Sprite interactIcon, Sprite InteractPrompt) {
        _interactIconImage.sprite = interactIcon;
        if (interactIcon == null)
            Destroy(_interactIconImage.gameObject);
        if(InteractPrompt != null)
            _interactPromptImage.sprite = InteractPrompt;
    }
    public void UpdateString() {
        var stringIcon = InputDevicePromptSystem.InsertPromptSprites("[Player/Interact]");
        //Debug.Log(stringIcon);
        _interactIconText.text = stringIcon;
    }
    private void Start() {

        SetPos();
        AnimationStart();
    }
    private void SetPos() {
        var size = _rect.sizeDelta.y;
        var pos = _rect.anchoredPosition;
        var newPosY = pos.y - size * 0.5f;
        pos.y = newPosY;
        _rect.localPosition = pos;
    }

    private void AnimationStart() {
        float speed = 0.3f;
        if (_canvasGroup == null || _rect == null) return;
        _canvasGroup.DOFade(1, speed);
        _rect.DOLocalMoveY(0, speed).SetEase(Ease.OutBack);
    }
    internal void Destroy(Action value) {

        float speed = 0.2f;
        if (_canvasGroup == null || _rect == null) return;
        _canvasGroup.DOFade(0, speed)
            .OnComplete(()=> value.Invoke());
    }
}