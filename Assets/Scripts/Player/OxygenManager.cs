using System;
using UnityEngine;
using static PlayerMovement;

public class OxygenManager : MonoBehaviour, IPlayerModule {

    private float maxOxygen;
    private float oxygenDepletionRate = 1f;   // Oxygen loss per second underwater
    private float currentOxygen;
    private float maxHealth = 7; // amount in seconds the player can survive with 0 oxygen 
    private float playerHealth;
    private PlayerManager _player;
    private bool _isInsideOxygenZone;
    private PlayerState _cachedState;
    private bool peepPlayed;
    private bool _initialized;
    private bool _oxygenDepleted;
    [SerializeField] private LowOxygenVisual _lowoxygenVisual;
    private LowOxygenVisual _lowoxygenVisualInstance;
    private bool _infOx;

    public static event Action<float, float> OnOxygenChanged;
    public static event Action OnFlashStart;
    public static event Action OnFlashStop;
    public int InitializationOrder => 92; // After playerstats

    public float CurrentOxygen { 
        get {
            return currentOxygen;
        }
        set {
            currentOxygen = Mathf.Clamp(value, 0, maxOxygen);
        }
    }
        

    public void InitializeOnOwner(PlayerManager playerParent) {
        maxOxygen= playerParent.PlayerStats.GetStat(StatType.PlayerOxygenMax);
        CurrentOxygen = maxOxygen;
        playerHealth = maxHealth;
        _player = playerParent;
        playerParent.PlayerStats.OnStatChanged += OnStatChanged;
        playerParent.PlayerMovement.OnPlayerStateChanged += StateChanged;
        _initialized = true;
    }
    private void OnDestroy() {
        _player.PlayerStats.OnStatChanged -= OnStatChanged;
        _player.PlayerMovement.OnPlayerStateChanged -= StateChanged;

    }

    private void OnStatChanged() {
        maxOxygen = _player.PlayerStats.GetStat(StatType.PlayerOxygenMax);
    }

    private void StateChanged(PlayerState newState) {
        _cachedState = newState;
    }

    private void Update() {
        if (!_initialized) return;
        if (ShouldDepleteOxygen()) {
            DepleteOxygen();
        } else {
            ReplenishOxygen();
        }
    }
    private bool ShouldDepleteOxygen() {
        if (_infOx) return false;
        if (_cachedState == PlayerState.Swimming) {
            if (_isInsideOxygenZone) {
                return false;
            } else {
                return true;
            }
        } else if (_cachedState == PlayerState.Grounded) {
            return false; // Replenish oxygen when grounded
        }
        return true;
    }

    void DepleteOxygen() {
        CurrentOxygen -= oxygenDepletionRate * Time.deltaTime;
        OnOxygenChanged?.Invoke(CurrentOxygen, maxOxygen);
        if (CurrentOxygen <= maxOxygen * 0.2f && !peepPlayed) { // 20% max oxygen
            if (AudioController.Instance != null) AudioController.Instance.PlaySound2D("PeepPeep", 1f);
            OnFlashStart?.Invoke();
            peepPlayed = true;
        }
        if (CurrentOxygen <= 0) {
            // Slowly fade out and then teleport player back to base?
            playerHealth -= Time.deltaTime;
            if (playerHealth <= 0) {
                _player.InventoryN.RemoveAll(); // womp womp 
                _player.PlayerLayerController.PutPlayerInSub();
                Resurect();
            }
        } else if(CurrentOxygen <= maxOxygen * 0.2f) {
            if (!_oxygenDepleted) {
                // First time reaching here spawn the depleting effect
                _oxygenDepleted = true;
                _lowoxygenVisualInstance = Instantiate(_lowoxygenVisual);
            }
        }
    }
    void ReplenishOxygen() {
        peepPlayed = false;
        OnFlashStop?.Invoke();
        if (_oxygenDepleted) {
            // Remove low oxygen effect 
            _oxygenDepleted = false;
            _lowoxygenVisualInstance.CancelAndRemove();
        }
        CurrentOxygen += oxygenDepletionRate * 50 * Time.deltaTime;
        playerHealth = maxHealth;
        OnOxygenChanged?.Invoke(CurrentOxygen, maxOxygen);
    }
    private void Resurect() {
        playerHealth = maxHealth;
        CurrentOxygen = maxOxygen;
    }
    public void GainOxygen(float amount) {
        CurrentOxygen += amount;
    }


    public void DEBUGSet0Oxygen() {
        CurrentOxygen = 0;
    }
    public void DEBUGPlayerPassOut() {
        CurrentOxygen = 1;
        playerHealth = 1;
    }
    public void DEBUGToggleinfOx() {
        if (_infOx) {
            // turn off 
            maxOxygen = _player.PlayerStats.GetStat(StatType.PlayerOxygenMax);
            CurrentOxygen = maxOxygen;
            _infOx = false;
        } else {
            maxOxygen = 9999999;
            CurrentOxygen = 999999;
            if(_lowoxygenVisualInstance != null) {
                _lowoxygenVisualInstance.CancelAndRemove();
            }
            _infOx = true;
        }
    }

}