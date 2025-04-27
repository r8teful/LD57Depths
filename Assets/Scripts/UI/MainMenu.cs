using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour {
    public AudioMixerGroup SFXMixer;
    public AudioMixerGroup MusicMixer;
    public Slider SFXSlider;
    public Slider MusicSlider;
    public void OnPlayClicked() {
        SceneManager.LoadScene(1);
    }
    public void OnSFXChanged(float v) {
        SFXMixer.audioMixer.SetFloat("sfx", Mathf.Log10(SFXSlider.value) * 20);
    }
    public void OnMuiscChanged(float v) {
        SFXMixer.audioMixer.SetFloat("music", Mathf.Log10(MusicSlider.value) * 20);
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
