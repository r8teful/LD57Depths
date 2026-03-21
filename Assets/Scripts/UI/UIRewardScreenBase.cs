using Coffee.UIExtensions;
using DG.Tweening;
using UnityEngine;

public abstract class UIRewardScreenBase : MonoBehaviour {

    [SerializeField] private Transform _screenContainer;
    [SerializeField] private Transform _rewardContainer;
    [SerializeField] private GameObject _reward;
    [SerializeField] private CanvasGroup _tint;
    [SerializeField] private UIParticle _particles;
    [SerializeField] private UIParticle _particlesChosen;
    private bool _isOpen;

    public bool IsOpen => _isOpen;
    protected void Start() {
        _tint.alpha = 0;
        _screenContainer.gameObject.SetActive(false);
        _isOpen = false;
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
                Debug.LogError("Reward missing IUIReward component!");
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
        _isOpen = true;
        var rewards = PlayerManager.Instance.PlayerReward.UpgradeEffects;
        SpawnRewardVisuals(rewards);
        _screenContainer.transform.localScale = Vector3.one * 0.1f;
        _screenContainer.gameObject.SetActive(true);
        _screenContainer.DOScale(1, 0.3f).SetEase(Ease.OutBack).SetUpdate(true);
        if (_particles != null) {
            _particles.gameObject.transform.DOKill();   
            _particles.gameObject.transform.rotation = Quaternion.identity;
            _particles.gameObject.transform.DORotate(new(0, 0, 4f),2).SetLoops(-1,LoopType.Incremental).SetUpdate(true).SetEase(Ease.Linear);
            _particles.Play();
        }
        _tint.alpha = 1;
    }
    public void HideScreen() {
        _screenContainer.gameObject.SetActive(false);
        _tint.alpha = 0;
        _isOpen = false;
        //_particlesChosen.Play();
        //_tint.DOFade(0, 0.3f);
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