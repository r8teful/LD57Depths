using Newtonsoft.Json.Bson;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class MiningBase : MonoBehaviour, IToolBehaviour {
    protected InputManager _inputManager;
    protected Coroutine miningRoutine;
    protected bool _isMining;
    protected bool _isUsingAbility;
    public float Range { get; set; }
    public float DamagePerHit { get; set; }
    public float RotationSpeed { get; set; }
    public float KnockbackStrength { get; set; }
    public float FalloffStrength { get; set; }
    public abstract object VisualData { get; }
    public GameObject GO => gameObject;
    public abstract ToolAbilityBaseSO AbilityData { get; }
    public abstract ToolType ToolType { get; }
    public ushort ToolID => (ushort)ToolType;


    private PlayerStatsManager _localPlayerStats; // The script is attached to a player, this is that players stats, could be our stats, or a remove clients stats

    public event Action<bool> AbilityStateChanged;

    // Only owner runs this
    public void Init(NetworkedPlayer owner) {
        _localPlayerStats = owner.PlayerStats;
        if (_localPlayerStats.IsInitialized) {
            // If stats are already ready (e.g., late join), grab them immediately.
            InitializeWithCurrentStats(_localPlayerStats);
        } else {
            _localPlayerStats.OnInitialized += HandleStatsInitialized; // Wait until initialized
        }

        _localPlayerStats.OnStatChanged += OnPlayerStatsChange;
    }
    private void InitializeWithCurrentStats(PlayerStatsManager pStats) {
        Range = pStats.GetStat(StatType.MiningRange);
        DamagePerHit = pStats.GetStat(StatType.MiningDamage);
        RotationSpeed = pStats.GetStat(StatType.MiningRotationSpeed);
        KnockbackStrength = pStats.GetStat(StatType.MiningKnockback);
        FalloffStrength = pStats.GetStat(StatType.MiningFalloff);
    }

    // IDK it knows its 180 so why the fuck can't I move the fucking laser?
    //private void FixedUpdate() {
    //    if (_localPlayerStats != null) {
    //        Debug.Log(_localPlayerStats.GetStat(StatType.MiningRotationSpeed));
    //    }
    //}

    private void HandleStatsInitialized() {
        InitializeWithCurrentStats(_localPlayerStats);
    }

    private void OnPlayerStatsChange(StatType stat, float newV) {
        if (stat == StatType.MiningDamage) {
            DamagePerHit = newV;
        }
        if (stat == StatType.MiningRange) {
            Range = newV;
        }
        if (stat == StatType.MiningRotationSpeed) {
            RotationSpeed = newV;
        }
        if (stat == StatType.MiningKnockback) {
            KnockbackStrength = newV;
        }

        Debug.Log($"New upgrade {stat} is: " + newV);
    }

    public void OnDestroy() {
        if(_localPlayerStats != null) {
            _localPlayerStats.OnStatChanged -= OnPlayerStatsChange;
        }
    }
    public virtual void ToolStart(InputManager input, ToolController controller) {
        if (miningRoutine != null) {
            Debug.LogWarning("Mining routine is still running even though it should have stopped!");
            StopCoroutine(miningRoutine);
        }
        _inputManager = input;
        Debug.Log("Mining true!");
        _isMining = true;
        if (_isUsingAbility) {
            //Debug.Log("ToolStart Ability");
            miningRoutine = StartCoroutine(MiningRoutineAbility(controller));
        } else {
            //Debug.Log("ToolStart NORMAL");
            miningRoutine = StartCoroutine(MiningRoutine(controller));
        }
    }
    public virtual void ToolStop(ToolController controller) {
        if (miningRoutine != null) {
            StopCoroutine(miningRoutine);
            miningRoutine = null;
            _isMining = false;
        }
    }
    public virtual IEnumerator MiningRoutine(ToolController controller) {
        while (true) {
            yield return new WaitForSeconds(0.1f); 
            if (!_isMining) yield break;
                
            var pos = _inputManager.GetAimWorldInput();
            //Debug.Log(pos);
            var isFlipped = false;
            var horizontalInput = _inputManager.GetMovementInput().x;

            CastRays(pos, controller, isFlipped); // Todo determine freq here
        }
    }
    public abstract IEnumerator MiningRoutineAbility(ToolController controller);
    public abstract void CastRays(Vector2 pos, ToolController controller, bool isFlipped);

    /* How abilities should work...
     * You press the ability button, then for X seconds, the tool uses other, modified stats and behaviours for a limited amount of time...
     * This means, everything else should stay the same, only the MiningRoutine should be different, right?
     */
    public void ToolAbilityStart(ToolController toolController) {
        if (_isUsingAbility) {
            Debug.LogWarning("Already using ability!");
            return;
        }
        StartAbility(toolController, AbilityData);
       
    }
    public void StartAbility(ToolController toolController,ToolAbilityBaseSO ability) {
        // Create the StatModifier instances from our ScriptableObject data
        var modifiersToAdd = new List<StatModifier>();
        foreach (var modData in ability.Modifiers) {
            // IMPORTANT: We use the ScriptableObject asset itself as the 'Source'.
            // This guarantees a unique and reliable ID to remove the modifiers later.
            modifiersToAdd.Add(new StatModifier(modData.Value, modData.Stat, modData.Type, ability));
        }
        // Add all modifiers to the player stats manager
        _localPlayerStats.AddModifiers(modifiersToAdd);

        // Start a coroutine to remove them after the duration
        StartCoroutine(AbilityCountDown(ability));
    }
    private IEnumerator AbilityCountDown(ToolAbilityBaseSO ability) {
        ChangeAbilityState(true);
        yield return new WaitForSeconds(ability.Duration); // Possible have it cancel when certain things happen? Don't think we have to though..
        Debug.Log("Ability wore off!");

        _localPlayerStats.RemoveModifiersFromSource(ability);
        ChangeAbilityState(false);
    }
    private void ChangeAbilityState(bool isUsing) {
        _isUsingAbility = isUsing;
        AbilityStateChanged?.Invoke(isUsing);
    }

    public abstract void OwnerUpdate();
}