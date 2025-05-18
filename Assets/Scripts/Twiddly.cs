using UnityEngine;

public class Twiddly : MonoBehaviour {
    [SerializeField] Sprite[] _twiddlies;
    [SerializeField] SpriteRenderer _spriteRenderer;
    private void Start() {
        _spriteRenderer.sprite = _twiddlies[Random.Range(0,3)];
    }
}