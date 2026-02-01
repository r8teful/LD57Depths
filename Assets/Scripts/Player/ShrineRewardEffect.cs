internal class ShrineRewardEffect : IExecutable {
    private readonly StatModifier stat;

    public ShrineRewardEffect(StatModifier stat) {
        this.stat = stat;
    }

    public void Execute(ExecutionContext context) {
        // Add buff...
        context.Player.PlayerStats.AddInstanceModifier(stat);
    }
}