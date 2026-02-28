using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour {
    [SerializeField] Button _buttonPlay;
    
    [SerializeField] private TMP_InputField _addressField;

    private void OnEnable() {
        _buttonPlay.onClick.AddListener(OnHostClicked);
    }
    private void OnDisable() {
        _buttonPlay.onClick.RemoveListener(OnHostClicked);
        // Unsubscribe from client connection events to avoid memory leaks.
    }

    // Starting new hosting game
    public void OnHostClicked() {
        SceneManager.LoadScene(1);
    }
    public void OnJoinClicked() {
        //SceneManager.LoadScene(1);
    }

    public void OnButtonYouTubeClick() {
        Application.OpenURL("https://www.youtube.com/@r8teful/featured");
    }
    public void OnButtonDiscordClick() {
        Application.OpenURL("https://discord.gg/A88Fg8cVm8");
    }
    public void OnButtonBlueSkyClick() {
        Application.OpenURL("https://bsky.app/profile/mouseandcatgames.bsky.social");
    }
    public void OnButtonWebsiteClick() {
        Application.OpenURL("https://mouseandcatgames.com");
    }
    public void OnButtonInstagramClick() {
        Application.OpenURL("https://www.instagram.com/mouseandcatgames/");
    }
    public void OnButtonSteamClick() {
        Application.OpenURL("https://store.steampowered.com/curator/44869972");
    }

}
