using System;
using TMPro;
using UnityEngine;

public class UILevelUpReward : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI _rewardText;
    internal void Init(IExecutable reward) {
        // Maybe these should spawn their own separate prefab visuals?
        if(reward is AddAbilityEffect a) {
            _rewardText.text = a.Ability.displayName;
        } else if (reward is UpgradeRecipeSO u) {
            _rewardText.text = u.displayName;
        } else {
            _rewardText.text = "TODO";
        }
    }
}
