using System;
using UnityEngine;
using static PlayerMovement;

public class OxygenManager : MonoBehaviour, INetworkedPlayerModule {

    public float maxOxygen = 250f;
    public float oxygenDepletionRate = 1f;   // Oxygen loss per second underwater
    private float currentOxygen;
    private float maxHealth = 15; // amount in seconds the player can survive with 0 oxygen 
    private float playerHealth;
    private NetworkedPlayer _player;
    private bool _isInsideOxygenZone;
    private PlayerState _cachedState;
    private bool peepPlayed;

    public static event Action<float, float> OnOxygenChanged;
    public int InitializationOrder => 92; // Don't think order matters here

    public float CurrentOxygen { 
        get {
            return currentOxygen;
        }
        set {
            currentOxygen = Mathf.Clamp(value, 0, maxOxygen);
        }
    }
        

    public void InitializeOnOwner(NetworkedPlayer playerParent) {
        CurrentOxygen = maxOxygen;
        playerHealth = maxHealth;
        _player = playerParent;
        playerParent.PlayerStats.OnStatChanged += OnStatChanged;
        playerParent.PlayerMovement.OnPlayerStateChanged += StateChanged;    
    }

    private void OnStatChanged() {
        maxOxygen = _player.PlayerStats.GetStat(StatType.PlayerOxygenMax);
    }

    private void StateChanged(PlayerState newState) {
        _cachedState = newState;
    }

    private void Update() {
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
        } else if (_cachedState == PlayerState.Grounded || _cachedState == PlayerState.ClimbingLadder) {
            return false; // Replenish oxygen when grounded
        }
        return true;
    }

    void DepleteOxygen() {
        CurrentOxygen -= oxygenDepletionRate * Time.deltaTime;
        OnOxygenChanged?.Invoke(CurrentOxygen, maxOxygen);
        if (CurrentOxygen <= 10 && !peepPlayed) {
            if (AudioController.Instance != null) AudioController.Instance.PlaySound2D("PeepPeep", 1f);
            peepPlayed = true;
            //SliderFlash(true);
        }
        if (CurrentOxygen <= 0) {
            // Slowly fade out and then teleport player back to base?
            playerHealth -= 1 * Time.deltaTime;
            if (playerHealth <= 0) {
                // Todo remove resources?
                Debug.LogWarning("No logic for resource removement");
                Resurect();
            }
            UpdateFadeOutVisual();
        }
    }
    void ReplenishOxygen() {
        peepPlayed = false;
        //SliderFlash(false);
        CurrentOxygen += oxygenDepletionRate * 50 * Time.deltaTime;
        playerHealth = maxHealth;
        OnOxygenChanged?.Invoke(CurrentOxygen, maxOxygen);
        UpdateFadeOutVisual();
    }
    private void Resurect() {
        playerHealth = maxHealth;
        CurrentOxygen = maxOxygen;
        UpdateFadeOutVisual();
    }
    public void GainOxygen(float amount) {
        CurrentOxygen += amount;
    }

    private void UpdateFadeOutVisual() {
        float healthRatio = playerHealth / maxHealth;
        float easedValue = 1 - Mathf.Pow(healthRatio, 2); // Quadratic ease-out

        //blackout.alpha = easedValue;
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