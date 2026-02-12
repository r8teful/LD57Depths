using TMPro;
using UnityEngine;

public class UIShrineReward : MonoBehaviour, IUIReward {
    [SerializeField] private UIUpgradeStat _statChange;
    [SerializeField] private Transform _statContainer;
    [SerializeField] private TextMeshProUGUI _statText;
    private UIRewardScreenBase _parent;
    private IExecutable _myReward;

    public void Init(UIRewardScreenBase parent, IExecutable reward) {
        if (reward is ShrineRewardEffect shrineReward) {
            DestroyChildren();
            var mod = shrineReward.GetModifier;
            var playerStats = PlayerManager.LocalInstance.PlayerStats;
            if (playerStats == null) {
                Debug.LogError("Couldnt find player stats!");
                return;
            }
            _statText.text = ResourceSystem.GetStatString(mod.Stat);
            // We might want to have several modifiers later?!?
            Instantiate(_statChange, _statContainer).Init(mod.GetStatus(playerStats),true);
            
        }
        _parent = parent;
        _myReward = reward;
    }

    private void DestroyChildren() {
        foreach (Transform item in _statContainer) {
            Destroy(item.gameObject);
        }
    }

    public void ButtonClick() {
        _parent.OnButtonClicked(_myReward);
    }

}