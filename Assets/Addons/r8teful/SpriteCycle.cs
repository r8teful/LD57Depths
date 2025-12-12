using System.Collections.Generic;
using UnityEngine;

public class SpriteCycle : MonoBehaviour {
    private SpriteRenderer _renderer;
    [SerializeField] private List<Sprite> _animationSprites;
    [SerializeField] private float _animationInterval;
    private int _animationIndex;
    private float _lastTickTime;

    private void Awake() {
        _renderer  = GetComponent<SpriteRenderer>();
    }
    private void Update() {
        if (_animationSprites != null) {
            if (Time.time >= _lastTickTime + (1f / _animationInterval)) {
                _renderer.sprite = _animationSprites[_animationIndex];
                _animationIndex++;
                // Loop
                if (_animationIndex >= _animationSprites.Count) {
                    _animationIndex = 0;
                }
                _lastTickTime = Time.time;
            }
        }
    }
}