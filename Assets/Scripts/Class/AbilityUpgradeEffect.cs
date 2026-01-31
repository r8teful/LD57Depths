internal class AbilityUpgradeEffect : IExecutable {
    private AbilityInstance _ability;
    private StatModifier statModifier;
    private RarityType _rarity;

    public RarityType Rarity => _rarity;
    public AbilityInstance Ability => _ability;
    public AbilityUpgradeEffect(AbilityInstance a, StatModifier statModifier, RarityType i) {
        this._ability = a;
        this.statModifier = statModifier;
        this._rarity = i; // used for ui
    }
    // same as StatModAbilityEffectSO
    public void Execute(ExecutionContext context) {
        _ability.AddInstanceModifier(statModifier);
    }

    // Again same as StatModAbilityEffectSO

    public StatChangeStatus GetChangeStatus() {
        var statName = ResourceSystem.GetStatString(statModifier.Stat);
       
        var currentValue = _ability.GetEffectiveStat(statModifier.Stat);
        var nextValue = _ability.GetEffectiveStat(statModifier.Stat, statModifier);

        return new(statName, currentValue.ToString("F2"), nextValue.ToString("F2"), ResourceSystem.IsLowerBad(statModifier.Stat));
    }
}