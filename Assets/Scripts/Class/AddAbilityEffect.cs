// This is basically just like an AbilityUnlockEffectSO but we can create it dynamically

public class AddAbilityEffect : IExecutable {
    private AbilitySO abilityToUnlock;
    public AbilitySO Ability => abilityToUnlock;
    public AddAbilityEffect(AbilitySO abilityToUnlock) {
        this.abilityToUnlock = abilityToUnlock;
    }

    public void Execute(ExecutionContext context) {
        NetworkedPlayer.LocalInstance.PlayerAbilities.AddAbility(abilityToUnlock);

    }
}