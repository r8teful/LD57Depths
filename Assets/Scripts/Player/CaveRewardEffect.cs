public class CaveRewardEffect : IExecutable {
    public EventCaveOutcomeType Type;
    IExecutable Effect; // Caves have different types of effect so we have to specify which one will be executed
    public CaveRewardEffect(EventCaveOutcomeType type) {
        Type = type;
    }

    public void Execute(ExecutionContext context) {
        Effect.Execute(context); // lol
    }

    public UIExecuteStatus GetExecuteStatus() {
        return Effect.GetExecuteStatus();
    }
}