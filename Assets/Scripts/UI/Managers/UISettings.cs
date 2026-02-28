using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Pause menu, settings, etc
public class UISettings : MonoBehaviour {
    [SerializeField] TMP_Dropdown _screenModeDropdown;
    [SerializeField] private Button _buttonGame;
    [SerializeField] private Button _buttonVideo;
    [SerializeField] private Button _buttonGraphics;
    [SerializeField] private Button _buttonAudio;
    [SerializeField] private Button _buttonApplyVideo;
    [SerializeField] private GameObject _containerGame;
    [SerializeField] private GameObject _containerVideo;
    [SerializeField] private GameObject _containerGraphics;
    [SerializeField] private GameObject _containerAudio;
    private FullScreenMode fullscreenMode;
    private bool didSave;

    private void Awake() {
        _buttonGame.onClick.AddListener(() => ShowPanel(_containerGame));
        _buttonVideo.onClick.AddListener(() => ShowPanel(_containerVideo));
        _buttonGraphics.onClick.AddListener(() => ShowPanel(_containerGraphics));
        _buttonAudio.onClick.AddListener(() => ShowPanel(_containerAudio));
        _buttonApplyVideo.onClick.AddListener(OnApplyVideoClick);
        _screenModeDropdown.onValueChanged.AddListener(OnScreenModeSet);
    }

    private void Start() {
        ShowPanel(_containerGame);
    }

    private void ShowPanel(GameObject panelToShow) {
        _containerGame.SetActive(false);
        _containerVideo.SetActive(false);
        _containerGraphics.SetActive(false);
        _containerAudio.SetActive(false);
        panelToShow.SetActive(true);
    }

    public void OnScreenModeSet(int i) {
        //var i = _screenModeDropdown.value;
        Debug.Log(i);
        if (i == 0) {
            fullscreenMode = FullScreenMode.FullScreenWindow;
        } else if (i == 1) {
            fullscreenMode = FullScreenMode.ExclusiveFullScreen;
        } else if (i == 2) {
            fullscreenMode = FullScreenMode.Windowed;
        }
        SaveSettings();
    }

    private void OnApplyVideoClick() {
        SaveSettings();
        didSave = true;
    }

    private void SaveSettings() {
        //PlayerPrefs.SetFloat("soundVolume", _audioSlider.value);
        Screen.fullScreenMode = fullscreenMode;
        PlayerPrefs.SetInt("ScreenMode", (int)Screen.fullScreenMode);
    }
    private void DisplayAmount() {
        //Display.displays.Length // Do that and then they can choose what monitor to use
    }

    internal void OnBack() {
        // Did we save? 
        if (didSave) {
            // No need to rewert
            didSave = false;
        } else {
            fullscreenMode = Screen.fullScreenMode;
        }
    }
}