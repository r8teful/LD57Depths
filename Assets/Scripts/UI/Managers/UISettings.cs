using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

// Pause menu, settings, etc
public class UISettings : MonoBehaviour {
    [SerializeField] TMP_Dropdown _screenModeDropdown;
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
}