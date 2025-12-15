public interface IEffectPassive {
    void Apply(AbilityInstance instance, NetworkedPlayer player);
    void Remove(AbilityInstance instance, NetworkedPlayer player);
}
public interface IEffectActive {
    // Called when an active ability is used (one-shot)
    void Execute(AbilityInstance source, NetworkedPlayer player);
}
public interface IEffectBuff {
    BuffSO Buff { get; }
}