using UnityEngine;

public class RandomSpriteSetter : MonoBehaviour {
    [SerializeField] Sprite[] _spritesVariants;
    [SerializeField] SpriteRenderer _spriteRenderer;
    private int _randomIndex = -1;
    public int RandomIndex => _randomIndex;
    private void Awake() {
        if (_spritesVariants == null || _spritesVariants.Length == 0)
            return;
        _randomIndex = Random.Range(0, _spritesVariants.Length);
    }
    private void Start() {
        if (_spriteRenderer == null) { 
            _spriteRenderer = GetComponent<SpriteRenderer>();
        }
        if (_randomIndex == -1) return;
        _spriteRenderer.sprite = _spritesVariants[_randomIndex];
    }
}