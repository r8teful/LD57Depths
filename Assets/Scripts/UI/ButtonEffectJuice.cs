using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
[RequireComponent (typeof(Button))]
public class ButtonEffectJuice : MonoBehaviour, IPointerEnterHandler {
    private Button _button;
    void Start() {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(Animation);

    }
    private void OnDestroy() {
        if(_button != null)
            _button.onClick.RemoveAllListeners();
    }
    private void Animation() {
        //App.AudioController.PlaySound2D("ButtonClick");
        var vibrato = 5;
        var elasticity = 1;
        var scale = -0.1f;
        _button.transform.localScale = Vector3.one;
        _button.transform.localRotation = Quaternion.identity;
        _button.transform.DOPunchScale(new(scale, scale, scale), 0.2f,vibrato,elasticity);
        _button.transform.DOPunchRotation(new(0, 0, Random.Range(-2f,2f)), 0.2f,vibrato,elasticity);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        Animation();
    }
}
