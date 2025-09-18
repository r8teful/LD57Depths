using FishNet.Object;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// Created and sent to the Visual part so that we know how to draw it properly
public struct MiningToolData {
    public float ToolRange;
    public float ToolWidth;
    public int toolTier;
    // Add more as needed
}
public abstract class MiningBase : NetworkBehaviour, IToolBehaviour {
    protected InputManager _inputManager;
    protected Coroutine miningRoutine;
    protected bool _isMining;
    protected bool _isUsingAbility;
    public float Range { get; set; }
    private float _rangeBeforeAbility;
    public float DamagePerHit { get; set; }
    public float RotationSpeed { get; set; }
    public float KnockbackStrength { get; set; }
    public float FalloffStrength { get; set; }
    public GameObject GO => gameObject;
    public IToolVisual ToolVisual { get; private set; }
    public abstract ToolAbilityBaseSO AbilityData { get; }
    public abstract ToolType ToolType { get; }
    public ushort ToolID => (ushort)ToolType;
    private PlayerStatsManager _localPlayerStats; // The script is attached to a player, this is that players stats, could be our stats, or a remove clients stats

    private void InitializeWithCurrentStats() {
        var pStats = NetworkedPlayer.LocalInstance.PlayerStats;
        Range = pStats.GetStat(StatType.MiningRange);
        DamagePerHit = pStats.GetStat(StatType.MiningDamage);
        RotationSpeed = pStats.GetStat(StatType.MiningRotationSpeed);
        KnockbackStrength = pStats.GetStat(StatType.MiningKnockback);
        FalloffStrength = pStats.GetStat(StatType.MiningFalloff);
    }
    private void Awake() {
        if (gameObject.TryGetComponent<IToolVisual>(out var c)) {
            ToolVisual = c;
        } else {
            Debug.LogError($"Could not find ToolVisual on {ToolType}, make sure the prefab has a toolVisual script!");
        }
    }
    public override void OnStartClient() {
        base.OnStartClient();
        Debug.Log("StartBase called on: " + ToolType);
        InitializeWithCurrentStats();
        InitVisualTool(this);
        if (NetworkedPlayersManager.Instance.TryGetPlayer(OwnerId, out var player)) {
            _localPlayerStats = player.PlayerStats;
            _localPlayerStats.OnStatChanged += OnPlayerStatsChange;
        }
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

    public override void OnStopClient() {
        base.OnStartClient();
        if(_localPlayerStats != null) {
            _localPlayerStats.OnStatChanged -= OnPlayerStatsChange;
        }
    }
    public MiningToolData GetToolData() {
        return new MiningToolData {
            ToolRange = Range, 
            ToolWidth = _isUsingAbility ? Mathf.Min(DamagePerHit * 0.3f,0.6f) : 0.05f * DamagePerHit,
            toolTier = 0 //TODO
        };
    }
    public void InitVisualTool(IToolBehaviour toolBehaviourParent) {
        ToolVisual.Init(toolBehaviourParent);
    }
    protected virtual void Update() {
        if (_isMining) {
            ToolVisual.HandleVisualUpdate(_inputManager.GetAimWorldInput(), _inputManager,_isUsingAbility);
        }
    }

    public virtual void ToolStart(InputManager input, ToolController controller) {
        if (miningRoutine != null) {
            Debug.LogWarning("Mining routine is still running even though it should have stopped!");
            StopCoroutine(miningRoutine);
        }
        _inputManager = input;
        _isMining = true;
        if (_isUsingAbility) {
            Debug.Log("ToolStart Ability");
            miningRoutine = StartCoroutine(MiningRoutineAbility(controller));
        } else {
            Debug.Log("ToolStart NORMAL");
            miningRoutine = StartCoroutine(MiningRoutine(controller));
        }
        ToolVisual.HandleVisualStart(controller.GetPlayerParent().PlayerVisuals);
    }
    public virtual void ToolStop(ToolController controller) {
        if (miningRoutine != null) {
            StopCoroutine(miningRoutine);
            miningRoutine = null;
            _isMining = false;
        }
        ToolVisual.HandleVisualStop(controller.GetPlayerParent().PlayerVisuals);
    }
    public virtual IEnumerator MiningRoutine(ToolController controller) {
        while (true) {
            yield return new WaitForSeconds(0.3f); 
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
        _isUsingAbility = true;
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
    public void ToolAbilityStop(ToolController toolController) {
        if (!_isUsingAbility)
            return;
        _isUsingAbility = false;
    }
    private IEnumerator AbilityCountDown(ToolAbilityBaseSO ability) {
        _isUsingAbility = true;
        yield return new WaitForSeconds(ability.Duration); // Possible have it cancel when certain things happen? Don't think we have to though..
        Debug.Log("Ability wore off!");

        _localPlayerStats.RemoveModifiersFromSource(ability);
        _isUsingAbility = false;
    }
}