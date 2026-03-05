using DG.Tweening;
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
            _backdrop.DOFade(1,_fadeDuration).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(_fadeDuration);
        } else {
            _backdrop.alpha = 1;
        }
    }

    public IEnumerator Release(bool withFade = true) {
        if (_backdrop == null) GetCanvas();
        _backdrop.transform.SetAsLastSibling();
        if (withFade) {
            _backdrop.DOFade(0, _fadeDuration).SetEase(Ease.InQuad);
            yield return new WaitForSeconds(_fadeDuration);
        } else {
            _backdrop.alpha = 0;
        }
    }
}