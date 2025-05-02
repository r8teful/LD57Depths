using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;

public class UIGenericSound : MonoBehaviour, IPointerEnterHandler, IPointerDownHandler {
    private Button _button;
    private void Start() {
        _button = GetComponent<Button>();
    }
    public void OnPointerDown(PointerEventData eventData) {
        if (_button != null) {
            if (!_button.interactable) return;
        }
        AudioController.Instance.PlaySound2D("MenuClickConfirm");
    }

    public void OnPointerEnter(PointerEventData eventData) {
        if (_button != null) {
            if (!_button.interactable) return;
        }
        AudioController.Instance.PlaySound2D("MenuClick", 0.5f,pitch: new AudioParams.Pitch(AudioParams.Pitch.Variation.VerySmall));
    }
}