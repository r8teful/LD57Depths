using System;
using System.Collections;
using UnityEngine;

public class MiningFish : MonoBehaviour,IInitializableAbility {
    private AbilityInstance _abilityInstance;
    private NetworkedPlayer _player;
    private bool _isShooting;
    private bool _wasShootingLastFrame;
    private float _cooldownRemaining;
    private MiningFishVisual _visual;
    [SerializeField] private Transform  _spawnPos;
    [SerializeField] private float FishCooldown;

    public void Init(AbilityInstance instance, NetworkedPlayer player) {
        _abilityInstance = instance;
        _player = player;
        _visual = GetComponent<MiningFishVisual>();
        _visual.Init(player);
    }

    private void Update() {
        HandleShootStateTransition();
        _isShooting = _player.InputManager.IsAllowedMiningUse();
        if (!_isShooting) return;
        _visual.HandleVisualUpdate();
        if (MineDelayCheck()) {
            Mine();
        }
    }

    private void Mine() {
        Vector2 toolPosition = transform.position;
        Vector2 pos = _player.InputManager.GetAimWorldInput();
        Vector2 targetDirection = (pos - toolPosition).normalized;

        // Calculate the angle in degrees from the target direction
        float angle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
        // Create a quaternion for the rotation (rotate around Z-axis for 2D)
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        // Instantiate the projectile with the calculated rotation
        AudioController.Instance.PlaySound2D("RPGShoot", 1);

        //var fish = Instantiate(App.ResourceSystem.GetPrefab<FishProjectile>("FishProjectile"), _spawnPos.position, rotation);
        var fish = Instantiate(App.ResourceSystem.GetPrefab<FishProjectileDumb>("FishProjectile"), _spawnPos.position, rotation);
        fish.Init(_player, targetDirection * 3);

    }

    private void HandleShootStateTransition() {
        // I hate bools
        if (_isShooting && !_wasShootingLastFrame) {
            OnStartShoot();
        }
        if (!_isShooting && _wasShootingLastFrame) {
            OnEndShoot();
        }
        _wasShootingLastFrame = _isShooting;
    }

    private void OnEndShoot() {
        //_visual.EndVisual();
    }

    private void OnStartShoot() {
       // _visual.StartVisual();
    }

    private bool MineDelayCheck() {
        _cooldownRemaining -= Time.deltaTime;
        if (_cooldownRemaining <= 0) {
            _cooldownRemaining = FishCooldown; // Reset cooldown
            return true;
        }
        return false;
    }
}