using System;
using System.Collections;
using UnityEngine;
public class UILevelUpScreen : MonoBehaviour {

    [SerializeField] private Transform _screenContainer;
    [SerializeField] private Transform _layoutGroup;
    [SerializeField] private UILevelUpReward _levelUpReward;
    

    private void Start() {
        XPEvents.OnLevelUpReady += ShowScreen;
        _screenContainer.gameObject.SetActive(false);
    }

    private void ShowScreen(int level) {
        // Get rewards 
        var rewards = NetworkedPlayer.LocalInstance.PlayerReward.UpgradeEffects;
        SpawnRewardVisuals(rewards);
        _screenContainer.gameObject.SetActive(true);
    }

    private void SpawnRewardVisuals(IExecutable[] rewards) {
        foreach (var reward in rewards) {
            Instantiate(_levelUpReward, _layoutGroup).Init(reward);
        }
    }

    private void HideScreen() {
        _screenContainer.gameObject.SetActive(false);
    }
    public void OnButtonClicked() {
        LevelReady();
    }

    private void LevelReady() {
        // Player has done its thing we are now ready to continue the game
        HideScreen();
        XPEvents.TriggerUIReady();
    }
}