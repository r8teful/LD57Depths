using System;
using UnityEngine;
using static PlayerMovement;

public class OxygenManager : MonoBehaviour, IPlayerModule {

    private float maxOxygen;
    private float OxygenDepletionRate { 
        // Oxygen loss per second underwater
        get {
            return _baseOxygenDepletion * _oxygenDrainMultiplier; 
        } 
    } 
    private float _baseOxygenDepletion = 1f;   
    private float currentOxygen;
    private float maxHealth = 2.362f; // amount in seconds the player can survive with 0 oxygen 
    private float playerHealth;
    private PlayerManager _player;
    private bool _isInsideOxygenZone;
    private PlayerState _cachedState;
    private bool peepPlayed;
    private bool _oxygenDepleted;
    [SerializeField] private LowOxygenVisual _lowoxygenVisual;
    private LowOxygenVisual _lowoxygenVisualInstance;
    private bool _infOx;
    private float _oxygenDrainMultiplier = 1f;

    public static event Action<float, float> OnOxygenChanged; // current, max
    public static event Action OnFlashStart;
    public static event Action OnFlashStop;
    public static event Action OnPassOut;
    public int InitializationOrder => 92; // After playerstats

    public bool IsFullOxygen => CurrentOxygen >= maxOxygen;

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
        PlayerWorldLayerController.OnPlayerWorldLayerChange += PlayerLayerChanged;
    }


    private void OnDestroy() {
        _player.PlayerStats.OnStatChanged -= OnStatChanged;
        _player.PlayerMovement.OnPlayerStateChanged -= StateChanged;
        PlayerWorldLayerController.OnPlayerWorldLayerChange -= PlayerLayerChanged;

    }

    private void OnStatChanged() {
        maxOxygen = _player.PlayerStats.GetStat(StatType.PlayerOxygenMax);
    }

    private void StateChanged(PlayerState newState) {
        _cachedState = newState;
    }
     private void PlayerLayerChanged(int index) {
        if(index == 0) {
            _oxygenDrainMultiplier = 1; // We'll multiply it when draining oxygen
        } else if(index == 1) {
            _oxygenDrainMultiplier = 1.5f;
        } else if (index ==2) {
            _oxygenDrainMultiplier = 2f;
        } else if (index ==3) {
            _oxygenDrainMultiplier = 3f;
        } else if (index == 3) {
            _oxygenDrainMultiplier = 5f;
        } else {
            _oxygenDrainMultiplier = 1;
        }
    }

    private void Update() {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.IsBooting) return;
        if (ShouldDepleteOxygen()) {
            DepleteOxygen();
        } else if(!IsFullOxygen) {
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
        // Say warning is at 10 oxygen, that would give us 10 seconds to get to the ship.
        var warningOxygen = maxOxygen * 0.2f * OxygenDepletionRate; 
        CurrentOxygen -= OxygenDepletionRate * Time.deltaTime;
        OnOxygenChanged?.Invoke(CurrentOxygen, maxOxygen);
        if (CurrentOxygen <= warningOxygen && !peepPlayed) { // 20% max oxygen
            if (AudioController.Instance != null) AudioController.Instance.PlaySound2D("PeepPeep", 1f);
            OnFlashStart?.Invoke();
            peepPlayed = true;
        }
        if (CurrentOxygen <= 0) {
            // Slowly fade out and then teleport player back to base?
            playerHealth -= Time.deltaTime;
            if (playerHealth <= 0) {
                OnPassOut?.Invoke(); // Important to call before we remove all 
                _player.InventoryN.RemoveAll(); // womp womp 
                _player.PlayerLayerController.PutPlayerInSub();
                Resurect();
            }
        } else if(CurrentOxygen <= warningOxygen) {
            if (!_oxygenDepleted) {
                // First time reaching here spawn the depleting effect
                _oxygenDepleted = true;
                _lowoxygenVisualInstance = Instantiate(_lowoxygenVisual);
                _lowoxygenVisualInstance.Play(warningOxygen); // Visual also changes sounds
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
            AudioController.Instance.UnMuffleLoop(0);
            AudioController.Instance.UnMuffleLoop(1); 
        }
        CurrentOxygen += OxygenDepletionRate * 50 * Time.deltaTime;
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