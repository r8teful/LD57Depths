using System;
using System.Collections;
using UnityEngine;
using static PlayerMovement;

public class OxygenManager : MonoBehaviour, IPlayerModule {

    private float maxOxygen;
    private float oxygenDepletionRate = 1f;   // Oxygen loss per second underwater
    private float currentOxygen;
    private float maxHealth = 15; // amount in seconds the player can survive with 0 oxygen 
    private float playerHealth;
    private PlayerManager _player;
    private bool _isInsideOxygenZone;
    private PlayerState _cachedState;
    private bool peepPlayed;
    public GameObject OxygenWarning;
    private bool _isFlashing;
    private Coroutine _flashCoroutine;
    private bool _initialized;
    private bool _oxygenDepleted;
    [SerializeField] private LowOxygenVisual _lowoxygenVisual;
    private LowOxygenVisual _lowoxygenVisualInstance;

    public static event Action<float, float> OnOxygenChanged;
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
        if (CurrentOxygen <= 15 && !peepPlayed) {
            if (AudioController.Instance != null) AudioController.Instance.PlaySound2D("PeepPeep", 1f);
            peepPlayed = true;
            //SliderFlash(true);
        }
        if (CurrentOxygen <= 0) {
            if (!_oxygenDepleted) {
                // First time reaching here spawn the depleting effect
                _oxygenDepleted = true;
                _lowoxygenVisualInstance = Instantiate(_lowoxygenVisual);
            }
            // Slowly fade out and then teleport player back to base?
            playerHealth -= 1 * Time.deltaTime;
            if (playerHealth <= 0) {
                _player.InventoryN.RemoveAll(); // womp womp 
                _player.PlayerLayerController.PutPlayerInSub();
                Resurect();
            }
        }
    }
    void ReplenishOxygen() {
        peepPlayed = false;
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

    // Call this function to start or stop the flashing
    public void SliderFlash(bool shouldFlash) {
        if (OxygenWarning == null) {
            Debug.LogWarning("SliderFlash: sliderToFlash GameObject is not assigned! Please assign it in the Inspector.");
            return;
        }

        if (shouldFlash) {
            if (!_isFlashing) // Don't start a new coroutine if already flashing
            {
                _isFlashing = true;
                _flashCoroutine = StartCoroutine(FlashCoroutine());
            }
        } else {
            if (_isFlashing) // Only stop if currently flashing
            {
                _isFlashing = false;
                StopCoroutine(_flashCoroutine);
                OxygenWarning.SetActive(false); // Ensure it's visible when stopping the flash
            }
        }
    }

    private IEnumerator FlashCoroutine() {
        while (_isFlashing) {
            // Toggle the active state of the GameObject
            OxygenWarning.SetActive(!OxygenWarning.activeSelf);

            // Wait for the flashSpeed duration
            yield return new WaitForSeconds(0.2f);
        }
    }


    public void DEBUGSet0Oxygen() {
        CurrentOxygen = 0;
    }
    public void DEBUGPlayerPassOut() {
        CurrentOxygen = 1;
        playerHealth = 1;
    }
    public void DEBUGinfOx() {
        maxOxygen = 9999999;
        CurrentOxygen = 999999;
    }

}