using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
[RequireComponent (typeof(Button))]
public class ButtonEffectJuice : MonoBehaviour {
    private Button _button;
    void Start() {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(OnButtonClick);
    }
    private void OnDestroy() {
        if(_button != null)
            _button.onClick.RemoveAllListeners();
    }
    private void OnButtonClick() {
        App.AudioController.PlaySound2D("ButtonClick");
        var vibrato = 5;
        var elasticity = 1;
        var scale = -0.1f;
        _button.transform.DOPunchScale(new(scale, scale, scale), 0.2f,vibrato,elasticity);
        _button.transform.DOPunchRotation(new(0, 0, Random.Range(-2f,2f)), 0.2f,vibrato,elasticity);
    }
}
