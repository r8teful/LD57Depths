public class UIChestRewardScreen : UIRewardScreenBase {
    private void Awake() {
        RewardEvents.OnChestOpen += ShowScreen;
    }
    public override void Resume(IExecutable choice) {
        base.Resume(choice); // executes the reward
        GameSequenceManager.Instance.AdvanceSequence(); // 
    }
}