using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

// Pause menu, settings, etc
public class UISettings : MonoBehaviour {
    [SerializeField] TMP_Dropdown _screenModeDropdown;
    public Slider SFXSlider;
    public Slider MusicSlider;
    public AudioMixerGroup SFXMixer;
    public AudioMixerGroup MusicMixer;
    // Set in inspector
    public void onScreenModeSet() {
        var i = _screenModeDropdown.value;
        Debug.Log(i);
        if (i == 0) {
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        } else if (i == 1) {
            Screen.fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        } else if (i == 2) {
            Screen.fullScreenMode = FullScreenMode.Windowed;
        }
        SaveSettings();
    }

    private void SaveSettings() {
        //PlayerPrefs.SetFloat("soundVolume", _audioSlider.value);
        PlayerPrefs.SetInt("ScreenMode", (int)Screen.fullScreenMode);
    }
    public void OnSFXChanged(float v) {
        SFXMixer.audioMixer.SetFloat("sfx", Mathf.Log10(SFXSlider.value) * 20);
    }
    public void OnMuiscChanged(float v) {
        SFXMixer.audioMixer.SetFloat("music", Mathf.Log10(MusicSlider.value) * 20);
    }
}