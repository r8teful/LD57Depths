/// <summary>
/// Passive effects are always active. But their individual logic can be fine tuned within the scripts they instantiate 
/// </summary>
public interface IEffectPassive {
    void Apply(AbilityInstance instance, PlayerManager player);
    void Remove(AbilityInstance instance, PlayerManager player);
}
/// <summary>
/// Active effects are executed only when specific events occur (button press/auto timer). They can have delays, and effect lengths
/// </summary>
public interface IEffectActive {
    void Execute(AbilityInstance source, PlayerManager player);
}
public interface IEffectBuff {
    BuffSO Buff { get; }
    float GetEffectiveStat(StatType stat, StatModifier tempMod = null);
}
// So we can have a generic "Add ability with prefab" SO
public interface IInitializableAbility {
    void Init(AbilityInstance instance, PlayerManager player);
}