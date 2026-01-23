using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UILevelUpReward : MonoBehaviour {
    [SerializeField] private List<Sprite> _images;
    [SerializeField] private TextMeshProUGUI _rewardText;
    [SerializeField] private TextMeshProUGUI _descriptionText;
    [SerializeField] private Button _button;
    [SerializeField] private Transform _statsChangeContainer;
    [SerializeField] private Image _boarderImage;
    private UILevelUpScreen _parent;
    private IExecutable _myReward;

    internal void Init(UILevelUpScreen uILevelUpScreen, IExecutable reward) {
        _parent = uILevelUpScreen;
        _myReward = reward;
        // Maybe these should spawn their own separate prefab visuals?
        if(reward is AddAbilityEffect a) {
            _boarderImage.sprite = _images[0];
            _rewardText.text = a.Ability.displayName;
            _descriptionText.text = a.Ability.description;
        } else if (reward is UpgradeRecipeSO u) {
            _boarderImage.sprite = _images[1];
            _rewardText.text = u.displayName;
            _descriptionText.text = "";
            foreach (var statData in u.GetStatStatuses()) {
                var statChange = Instantiate(App.ResourceSystem.GetPrefab<UIUpgradeStat>("UIUpgradeStatPopup"), _statsChangeContainer);
                statChange.Init(statData);
            }
        } else if(reward is AbilityUpgradeEffect ue) {
            // Create boarder for rarity
            _boarderImage.sprite = _images[2];
            _rewardText.text = $"{ue.Ability.Data.displayName} Upgrade with rarity: {ue.Rarity}";
            _descriptionText.text = "";
            var statData = ue.GetChangeStatus(); // Just suporting one change status. Because upgrade upgrade one stat for now
            var statChange = Instantiate(App.ResourceSystem.GetPrefab<UIUpgradeStat>("UIUpgradeStatPopup"), _statsChangeContainer);
            statChange.Init(statData);
            
        }
        _button.onClick.AddListener(ButtonClick);
    }
    private void OnDestroy() {
        _button.onClick.RemoveListener(ButtonClick);
    }
    public void ButtonClick() {
        _parent.OnButtonClicked(this, _myReward);
    }
}