using DG.Tweening;
using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class BackdropManager : Singleton<BackdropManager> {

    public RectTransform _backdropRect;
    public CanvasGroup  _backdrop;
    public TextMeshProUGUI _loadingText;
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
            _backdrop.DOFade(1, _fadeDuration).SetEase(Ease.OutQuad).SetUpdate(true);
            if(_loadingText !=null)
                _loadingText.DOFade(0, 4f).SetEase(Ease.OutQuint).SetLoops(-1, LoopType.Yoyo).SetUpdate(true);
            yield return new WaitForSecondsRealtime(_fadeDuration);
        } else {
            _backdrop.alpha = 1;
        }
    }

    public IEnumerator Release(bool withFade = true) {
        if (_backdrop == null) yield break;
        _backdrop.transform.SetAsLastSibling();
        if (withFade) {
            _backdrop.DOFade(0, _fadeDuration).SetEase(Ease.InQuad).SetUpdate(true);
            if (_loadingText != null)
                _loadingText.DOKill();
            yield return new WaitForSecondsRealtime(_fadeDuration);
        } else {
            _backdrop.alpha = 0;
        }
    }
}