using DG.Tweening;
using System;
using UnityEngine;

public class FogLayerVisual : MonoBehaviour {
    [SerializeField] private int _layerIndex;
    private SpriteRenderer _spriteRenderer;
    private bool _isShown;
    private float _startAlpha;
    private void OnEnable() {
        PlayerWorldLayerController.OnPlayerWorldLayerChange += PlayerLayerChanged;
    }
    private void OnDisable() {
        PlayerWorldLayerController.OnPlayerWorldLayerChange -= PlayerLayerChanged;
    }
    private void Awake() {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null) Debug.LogError("Component needs spriteRenderer!");
        _startAlpha = _spriteRenderer.color.a;
    }

    private void PlayerLayerChanged(int layer) {
        if (layer != _layerIndex) {
            // make sure we are faded in
                _spriteRenderer.DOKill();
                _spriteRenderer.DOFade(_startAlpha, 4f).OnComplete(()=> _isShown = false);
            return;
        }

        _spriteRenderer.DOFade(0, 4f).OnComplete(()=> _isShown = true);
    }
}
