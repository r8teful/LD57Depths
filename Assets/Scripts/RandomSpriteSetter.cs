using UnityEngine;

public class RandomSpriteSetter : MonoBehaviour {
    [SerializeField] Sprite[] _spritesVariants;
    [SerializeField] SpriteRenderer _spriteRenderer;
    private void Start() {
        _spriteRenderer.sprite = _spritesVariants[Random.Range(0, _spritesVariants.Length)];
    }
}