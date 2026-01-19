using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAbilities : MonoBehaviour, INetworkedPlayerModule {
    // runtime map from id -> instance
    [ShowInInspector]
    private Dictionary<ushort, AbilityInstance> _abilities = new();
    private HashSet<ushort> _ownedAbilities = new HashSet<ushort>();
    private NetworkedPlayer _player;
    [SerializeField] private Transform _abilitySlotTransform;
    public Transform AbilitySlot => _abilitySlotTransform;
    public HashSet<ushort> OwnedAbilities => _ownedAbilities;
    public int InitializationOrder => 999; // Has to be after player movement because some abilities need it

    public event Action<AbilityInstance> OnabilityRemove;
    public event Action<AbilityInstance> OnAbilityAdd;

    public void InitializeOnOwner(NetworkedPlayer playerParent) {
        _player = playerParent;
        AddAbility(App.ResourceSystem.GetAbilityByID(0)); // Lazer is ID 0 
        //AddAbility(App.ResourceSystem.GetAbilityByID(ResourceSystem.BrimstoneBuffID)); // Lazer blast
        AddAbility(App.ResourceSystem.GetAbilityByID(ResourceSystem.BiomeBuffID)); // Biome buffs
        //AddAbility(App.ResourceSystem.GetAbilityByID(101)); // cactus suit
        //AddAbility(App.ResourceSystem.GetAbilityByID(ResourceSystem.BlockOxygenID));
        //AddAbility(App.ResourceSystem.GetAbilityByID(ResourceSystem.PlayerDashID));
        //AddAbility(App.ResourceSystem.GetAbilityByID(ResourceSystem.ShockwaveID));
        //AddAbility(App.ResourceSystem.GetAbilityByID(ResourceSystem.BlackholeID));
        //AddAbility(App.ResourceSystem.GetAbilityByID(ResourceSystem.BouncingBallID));
        //AddAbility(App.ResourceSystem.GetAbilityByID(ResourceSystem.FishShooterID));
        //AddAbility(App.ResourceSystem.GetAbilityByID(69)); // Fish gun
    }

    public void AddAbility(AbilitySO data) {
        if (_abilities.ContainsKey(data.ID)) return;
        var inst = new AbilityInstance(data, _player);
        _abilities[data.ID] = inst;
        _ownedAbilities.Add(data.ID);
        // For passives, apply passive effects now
        if (data.type == AbilityType.Passive) {
            foreach (var so in data.effects)
                if (so is IEffectPassive pe)
                    pe.Apply(inst, _player);
        }
        OnAbilityAdd?.Invoke(inst);
    }
    public void RemoveAbility(AbilitySO data) {
        // No clue if this ever will happen, need to test first
        Debug.LogWarning("REMOVING ABILITY NOT TESTED TEST IT FIRST PLEASE1!!");
        if (!_abilities.TryGetValue(data.ID, out var inst)) return;
        // For passives, apply passive effects now
        if (data.type == AbilityType.Passive) {
            foreach (var so in data.effects)
                if (so is IEffectPassive pe)
                    pe.Remove(inst, _player);
        }
        _abilities.Remove(data.ID);
        _ownedAbilities.Remove(data.ID);
        OnabilityRemove?.Invoke(inst);
    }
    public bool HasAbility(ushort id) => _ownedAbilities.Contains(id);

    public AbilityInstance GetAbilityInstance(ushort id) {
        _abilities.TryGetValue(id, out var inst);
        return inst;
    }

    void Update() {
        float dt = Time.deltaTime;
        foreach (var inst in _abilities.Values)
            inst.Tick(dt);
    }

    // When we press space:
    public bool UseActive(ushort id) {
        var inst = GetAbilityInstance(id);
        if (inst == null) return false;
        return inst.Use(() => {
            // performEffect provided but we delegate to effects
            foreach (var so in inst.Data.effects)
                if (so is IEffectActive effect) effect.Execute(inst, _player);
            return true;
        });
    }

    private void ApplyPassive(AbilitySO data) {
        // Example hooks:
        // - Increase player's damage
        // - Modify movement speed
        // Keep passive effect code centralized (or data-driven)
        // e.g., if you have IPassiveEffect components on ScriptableObject, call data.Apply(player)
    }

    private void QueuePendingUpgradeForAbility(UpgradeRecipeSO up, StatModifier mod) {
        // store pending to apply later when ability added
        // For brevity, omitted implementation; simple map abilityId->List<StatModifier>
    }

    internal bool IsBrimstoneAbilityActive() {
        // Simple, just check if we have the brimstone buff on the ability
        return AbilityHasBuff(ResourceSystem.LazerEffectID, ResourceSystem.BrimstoneBuffID);
    }

    private bool AbilityHasBuff(ushort abilityID, ushort buffID) {
        var inst = GetAbilityInstance(abilityID);
        if (inst == null) return false;
        return inst.HasBuff(buffID); // I'm so smart, and your so good at pixel i love you hug4art
    }
}