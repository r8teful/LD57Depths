using UnityEngine;

public class SubVisualUpgradeable : MonoBehaviour {
    [SerializeField] private Sprite _spriteToSet;
    [SerializeField] private UpgradeNodeSO _nodeUpgrade;
    private SpriteRenderer _spriteRenderer;
    [SerializeField] private ParticleSystem _fixParticles;

    private void Awake() {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null) {
            Debug.LogError("Script needs renderer component to work!");
        }
        SubmarineManager.OnSubUpgrade += UpgradePurchased;

    }
    private void OnDestroy() {
        SubmarineManager.OnSubUpgrade -= UpgradePurchased;
    }


    private void UpgradePurchased(ushort upgrade) {
        if(upgrade == _nodeUpgrade.ID) {
            _spriteRenderer.sprite = _spriteToSet;
            if (_fixParticles != null) {
                _fixParticles.Play();
            }
        }
    }
}