using System.Collections;
using UnityEngine;
public class UILevelUpScreen : MonoBehaviour {

    [SerializeField] private Transform _screenContainer;


    private void Start() {
        XPEvents.OnLevelUpReady += ShowScreen;
        _screenContainer.gameObject.SetActive(false);
    }

    private void ShowScreen(int level) {
        _screenContainer.gameObject.SetActive(true);
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