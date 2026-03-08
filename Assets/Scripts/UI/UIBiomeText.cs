using DG.Tweening;
using TMPro;
using UnityEngine;

public class UIBiomeText : MonoBehaviour {
    private RectTransform _rect;
    private TextMeshProUGUI _text;
    private float _startY;
    private readonly float _offScreenY = -135f;

    private void Awake() {
        _rect = GetComponent<RectTransform>();
        _text = GetComponent<TextMeshProUGUI>();
        if (_rect == null || _text == null)
            Debug.LogError("Can't find component!");
        _startY = _rect.anchoredPosition.y;
        Debug.Log("StartY: " + _startY);
        
        // Set it offscreen then start animating in start
        var r = _rect.anchoredPosition;
        r.y = _offScreenY;
        _rect.anchoredPosition = r;
    }
    public void StartAnim(BiomeType biome) {
        var biomeData = App.ResourceSystem.GetBiomeData(biome);
        if(biomeData != null) {
            _text.color = biomeData.BiomeTextColour;
        }
        var biomeString = ResourceSystem.BiomeToString(biome);
        _text.text = biomeString;
        Sequence s = DOTween.Sequence();
        s.Append(_rect.DOAnchorPosY(_startY, 2).SetEase(Ease.OutBack));
        s.AppendInterval(6f); // wait for 6 sec
        s.Append(_rect.DOAnchorPosY(_offScreenY, 0.5f).SetEase(Ease.InQuad));
        s.OnComplete(()=> Destroy(gameObject));
    }

}
