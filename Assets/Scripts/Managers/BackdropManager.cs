using Pixelplacement;
using System.Collections;
using UnityEngine;

public class BackdropManager : Singleton<BackdropManager> {

    private CanvasGroup _backdrop; 
    private float _fadeDuration = 0.1f;
    private void GetCanvas() {
        _backdrop = GameObject.Find("CanvasApp").GetComponent<CanvasGroup>();
    }

    public IEnumerator Require(bool withFade = true) {
        if (_backdrop == null) GetCanvas();
        _backdrop.transform.SetAsLastSibling();
        if (withFade) {
            Tween.CanvasGroupAlpha(_backdrop, 1, _fadeDuration, 0,Tween.EaseOutStrong);
            yield return new WaitForSeconds(_fadeDuration);
        } else {
            _backdrop.alpha = 1;
        }
    }

    public IEnumerator Release(bool withFade = true) {
        if (_backdrop == null) GetCanvas();
        _backdrop.transform.SetAsLastSibling();
        if (withFade) {
            Tween.CanvasGroupAlpha(_backdrop, 0, _fadeDuration, 0,Tween.EaseInStrong);
            yield return new WaitForSeconds(_fadeDuration);
        } else {
            _backdrop.alpha = 0;
        }
    }
}