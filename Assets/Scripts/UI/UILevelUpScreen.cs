public class UILevelUpScreen : UIRewardScreenBase {

    private void Awake() {
        RewardEvents.OnLevelUpReady += ShowScreen;
        
    }

    private void ShowScreen(int obj) {
        base.ShowScreen();
    }

    public void OnSkippClicked() {
        Resume(null);
    }

    public override void Resume(IExecutable choice) {
        base.Resume(choice); 
        RewardEvents.TriggerUIReady();  // this calls GameSequenceManager.Instance.AdvanceSequence();
    }
}