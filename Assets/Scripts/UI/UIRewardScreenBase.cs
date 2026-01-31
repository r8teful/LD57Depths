using UnityEngine;

public abstract class UIRewardScreenBase : MonoBehaviour {

    [SerializeField] private Transform _screenContainer;
    [SerializeField] private Transform _rewardContainer;
    [SerializeField] private GameObject _reward;

    protected void Start() {
        _screenContainer.gameObject.SetActive(false);
    }
    private void SpawnRewardVisuals(IExecutable[] rewards) {
        foreach (Transform t in _rewardContainer) {
            Destroy(t.gameObject);
        }
        foreach (var reward in rewards) {
            var g = Instantiate(_reward, _rewardContainer);
            if (g.TryGetComponent<IUIReward>(out var r)) {
                r.Init(this, reward);
            } else {
                Debug.LogError("Rward missing IUIReward component!");
            }
        }
    }
    // These tree public methods is really the only thing that we need to call
    // It will be different depending on what level up screen we use
    // But the actual core of it is the same 
    // 1. Get the rewards from playerRewards
    // 2. Spawn the rewards, (however they look like, that is why its simply a very generic IUIReward)
    // 3. Show rewards
    // 4. Handle whatever happends when we click on one of the rewards
    public void ShowScreen() {
        var rewards = PlayerManager.LocalInstance.PlayerReward.UpgradeEffects;
        SpawnRewardVisuals(rewards);
        _screenContainer.gameObject.SetActive(true);
    }
    public void HideScreen() {
        _screenContainer.gameObject.SetActive(false);
    }
    
    public void OnButtonClicked(IExecutable choice) {
        // Execute the reward?
        Resume(choice);
    }


    public virtual void Resume(IExecutable choice) {
        // Resume logic could depend on what screen 
        // Tell reward manager to execute the reward we have chosen
        PlayerManager.Instance.PlayerReward.ExecuteReward(choice);
        HideScreen();
    } 
}