using System;
using UnityEngine;

public class SubVisualUpgradeable : MonoBehaviour {
    [SerializeField] private Sprite _spriteToSet;
    [SerializeField] private UpgradeNodeSO _nodeUpgrade;
    private SpriteRenderer _spriteRenderer;
    
    private void Awake() {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        if (_spriteRenderer == null) {
            Debug.LogError("Script needs renderer component to work!");
        }
        GameSetupManager.OnSetupComplete += MyAwake;

    }
    private void OnDestroy() {
        GameSetupManager.OnSetupComplete -= MyAwake;
        if (PlayerManager.Instance == null)
            PlayerManager.Instance.UpgradeManager.OnUpgradePurchased -= UpgradePurchased;
    }

    private void MyAwake() {
        if (PlayerManager.Instance == null) {
            Debug.LogError("Can't find player!!");
        }
        PlayerManager.Instance.UpgradeManager.OnUpgradePurchased += UpgradePurchased;
    }

    private void UpgradePurchased(UpgradeNodeSO upgrade) {
        if(upgrade == _nodeUpgrade) {
            _spriteRenderer.sprite = _spriteToSet;
        }
    }
}