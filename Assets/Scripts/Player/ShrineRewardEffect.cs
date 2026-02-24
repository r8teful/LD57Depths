internal class ShrineRewardEffect : IExecutable {
    private readonly StatModifier _stat;
    public StatModifier GetModifier => _stat;
    public ShrineRewardEffect(StatModifier stat) {
        _stat = stat;
    }

    public void Execute(ExecutionContext context) {
        // Add buff...
        context.Player.PlayerStats.AddInstanceModifier(_stat);
    }

    public UIExecuteStatus GetExecuteStatus() {
        return null;
    }
}