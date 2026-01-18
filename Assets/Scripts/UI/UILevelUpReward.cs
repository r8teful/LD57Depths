using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UILevelUpReward : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI _rewardText;
    [SerializeField] private Button _button;
    private UILevelUpScreen _parent;
    private IExecutable _myReward;

    internal void Init(UILevelUpScreen uILevelUpScreen, IExecutable reward) {
        _parent = uILevelUpScreen;
        _myReward = reward;
        // Maybe these should spawn their own separate prefab visuals?
        if(reward is AddAbilityEffect a) {
            _rewardText.text = a.Ability.displayName;
        } else if (reward is UpgradeRecipeSO u) {
            _rewardText.text = u.displayName;
        } else {
            _rewardText.text = "TODO";
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