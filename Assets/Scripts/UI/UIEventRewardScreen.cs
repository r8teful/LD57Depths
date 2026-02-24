public class UIEventRewardScreen : UIRewardScreenBase {
    private void Awake() {
        RewardEvents.OnCaveOpen += ShowScreen;
    }
    public override void Resume(IExecutable choice) {
        base.Resume(choice); // executes the reward
        GameSequenceManager.Instance.AdvanceSequence(); // 
    }
}