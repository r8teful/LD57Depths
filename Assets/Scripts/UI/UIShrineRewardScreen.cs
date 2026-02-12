public class UIShrineRewardScreen : UIRewardScreenBase {
    private void Awake() {
        RewardEvents.OnShrineOpen += ShowScreen;
    }
    private void OnDestroy() {
        RewardEvents.OnShrineOpen -= ShowScreen;
    }
    public override void Resume(IExecutable choice) {
        base.Resume(choice); // executes the reward
        GameSequenceManager.Instance.AdvanceSequence(); // 
    }
}