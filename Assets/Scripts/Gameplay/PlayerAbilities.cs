using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAbilities : MonoBehaviour, INetworkedPlayerModule {
    // runtime map from id -> instance
    private Dictionary<ushort, AbilityInstance> _abilities = new();
    private NetworkedPlayer _player;
    [SerializeField] private Transform _abilitySlotTransform;
    public Transform AbilitySlot => _abilitySlotTransform;
    public int InitializationOrder => 998; // Idk just do it last?

    public void InitializeOnOwner(NetworkedPlayer playerParent) {
        _player = playerParent;
        AddAbility(App.ResourceSystem.GetAbilityByID(0)); // Lazer is ID 0 
        AddAbility(App.ResourceSystem.GetAbilityByID(ResourceSystem.BrimstoneBuffID)); // Lazer blast
        //AddAbility(App.ResourceSystem.GetAbilityByID(69)); // Fish gun
    }

    public void AddAbility(AbilitySO data) {
        if (_abilities.ContainsKey(data.ID)) return;
        var inst = new AbilityInstance(data, _player);
        _abilities[data.ID] = inst;

        // For passives, apply passive effects now
        if (data.type == AbilityType.Passive) {
            foreach (var so in data.effects)
                if (so is IEffectPassive pe)
                    pe.Apply(inst, _player);
        }

        // Hook: apply queued upgrades targeting this ability, etc.
    }
    public bool HasAbility(ushort id) => _abilities.ContainsKey(id);

    public AbilityInstance GetInstance(ushort id) {
        _abilities.TryGetValue(id, out var inst);
        return inst;
    }

    void Update() {
        float dt = Time.deltaTime;
        foreach (var inst in _abilities.Values)
            inst.Tick(dt);
    }

    

    public bool UseActive(ushort id) {
        var inst = GetInstance(id);
        if (inst == null) return false;
        return inst.Use(() => {
            // performEffect provided but we delegate to effects
            foreach (var so in inst.data.effects)
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
    // Upgrading abilities...
    public void ApplyUpgrade(UpgradeRecipeSO up) {
        //var src = Guid.NewGuid(); // creates a unique source id for this application (or use up.upgradeId)
        
        /* We implement this later, core idea is that we need to add a statModifer TO the ability instance,  
         we'll have to loop through all the upgrade effects and somehow apply them to the upgrade, or find which abilities are effected, idk
        surelly the upgrade recipe will be specifically for an ability, so we would know that we could apply the upgrade to it

        var mod = new StatModifier {
            sourceId = src,
            stat = up.stat,
            op = up.op,
            value = up.value,
            expiresAt = up.duration <= 0f ? Mathf.Infinity : Time.time + up.duration
        };

        if (up.targetIsAbility) {
            if (_abilities.TryGetValue(up.targetAbilityId, out var inst)) {
                inst.AddInstanceModifier(mod);
                inst.OnModifiersChanged?.Invoke();
            } else {
                // If ability isn't present yet, you may want to queue the upgrade to apply when the ability is added
                // Example: store pending upgrades keyed by ability id
                QueuePendingUpgradeForAbility(up, mod);
            }
        } else {
            // global stat/buff upgrade
            _stats.AddModifier(mod);
        }
         */ 
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
        var inst = GetInstance(abilityID);
        if (inst == null) return false;
        return inst.HasBuff(buffID); // I'm so smart, and your so good at pixel i love you hug4art
    }
}