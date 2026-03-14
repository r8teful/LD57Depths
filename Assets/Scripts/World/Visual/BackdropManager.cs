using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;

public class BackdropManager : Singleton<BackdropManager> {

    public RectTransform _backdropRect; 
    public CanvasGroup  _backdrop; 
    private float _fadeDuration = 0.1f;
    
    public void DoWaveTransition(bool isReverce, Action onComplete) {
        int position = 2637;
        float time = 0.8f;
        var seq = DOTween.Sequence();
        if (isReverce) { 
            _backdropRect.anchoredPosition = new(0, position);
            seq.Append(_backdropRect.DOAnchorPosY(0, time)).SetEase(Ease.OutSine);
            seq.InsertCallback(time*0.3f,()=>onComplete?.Invoke()); // so cool!!!
        } else {
            _backdropRect.anchoredPosition = Vector2.zero;
            seq.Append(_backdropRect.DOAnchorPosY(position, time)).SetEase(Ease.OutSine);
            seq.InsertCallback(time * 0.3f, () => onComplete?.Invoke()); // so cool!!!

        }
    }

    public IEnumerator Require(bool withFade = true) {
        if (_backdrop == null) yield break;
        _backdrop.transform.SetAsLastSibling();
        if (withFade) {
            _backdrop.DOFade(1,_fadeDuration).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(_fadeDuration);
        } else {
            _backdrop.alpha = 1;
        }
    }

    public IEnumerator Release(bool withFade = true) {
        if (_backdrop == null) yield break;
        _backdrop.transform.SetAsLastSibling();
        if (withFade) {
            _backdrop.DOFade(0, _fadeDuration).SetEase(Ease.InQuad);
            yield return new WaitForSeconds(_fadeDuration);
        } else {
            _backdrop.alpha = 0;
        }
    }
}