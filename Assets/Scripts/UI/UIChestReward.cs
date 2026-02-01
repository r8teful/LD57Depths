using UnityEngine;
public class UIChestReward : MonoBehaviour, IUIReward {
    [SerializeField] private UIItemQuantity _itemQuanity;
    [SerializeField] private Transform _itemContainer;
    private UIRewardScreenBase _parent;
    private IExecutable _myReward;

    public void Init(UIRewardScreenBase parent, IExecutable reward) {
        if(reward is ChestRewardEffect chestReward) {
            var r = chestReward.GetRewards();
            foreach (var item in r) {
                Instantiate(_itemQuanity, _itemContainer).Init(item);
            }
        }
        _parent = parent;
        _myReward = reward;
    }
    public void ButtonClick() {
        _parent.OnButtonClicked(_myReward);
    }
}