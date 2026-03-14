using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
[RequireComponent (typeof(Button))]
public class ButtonEffectJuice : MonoBehaviour, IPointerEnterHandler {
    private Button _button;
    [SerializeField] private Transform _animTransform;
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
        Transform trans;
        var vibrato = 5;
        var elasticity = 1;
        var scale = -0.1f;
        if (_animTransform != null) { 
            trans = _animTransform;
        } else {
            trans = _button.transform;
        }
        trans.localScale = Vector3.one;
        trans.localRotation = Quaternion.identity;
        trans.DOPunchScale(new(scale, scale, scale), 0.2f,vibrato,elasticity).SetUpdate(true);
        trans.DOPunchRotation(new(0, 0, Random.Range(-2f,2f)), 0.2f,vibrato,elasticity).SetUpdate(true);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        Animation();
    }
}
