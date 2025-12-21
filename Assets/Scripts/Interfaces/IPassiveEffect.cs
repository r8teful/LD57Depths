/// <summary>
/// Passive effects are always active. But their individual logic can be fine tuned within the scripts they instantiate 
/// </summary>
public interface IEffectPassive {
    void Apply(AbilityInstance instance, NetworkedPlayer player);
    void Remove(AbilityInstance instance, NetworkedPlayer player);
}
/// <summary>
/// Active effects are executed only when specific events occur (button press/auto timer). They can have delays, and effect lengths
/// </summary>
public interface IEffectActive {
    void Execute(AbilityInstance source, NetworkedPlayer player);
}
public interface IEffectBuff {
    BuffSO Buff { get; }
}