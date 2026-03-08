using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Pause menu, settings, etc
public class UISettings : MonoBehaviour {
    [SerializeField] TMP_Dropdown _screenModeDropdown;
    [SerializeField] private Button _buttonGame;
    [SerializeField] private Button _buttonVideo;
    [SerializeField] private Button _buttonGraphics;
    [SerializeField] private Button _buttonAudio;
    [SerializeField] private Button _buttonApplyVideo;
    [SerializeField] private Button _buttonBack;
    
    // Actual setting stuff
    [SerializeField] private Toggle _debugMenu;
    [SerializeField] private Toggle _backgroundFancy;
    private FullScreenMode fullscreenMode;
    private bool didSave;
    private bool _fromPause;

    public UnityEvent<bool> OnSettingChange;
    private void Awake() {
        _buttonBack.onClick.AddListener(OnBackButtonClicked);
        _buttonApplyVideo.onClick.AddListener(OnApplyVideoClick);
        _screenModeDropdown.onValueChanged.AddListener(OnScreenModeSet);
        _debugMenu.onValueChanged.AddListener(OnDebugMenuChange);
        _backgroundFancy.onValueChanged.AddListener(OnBackgroundFancyChanage);
    }


    private void Start() {
        OnDebugMenuChange(false);
    }
    private void OnDebugMenuChange(bool isActive) {
        if (UIManager.Instance == null) return;
        if (isActive) {
            UIManager.Instance.DebugStatsShow();
        } else {
            UIManager.Instance.DebugStatsHide();
        }
    }
    private void OnBackgroundFancyChanage(bool isActive) {
        if (isActive) {

        } else {

        }
    }

    private void OnBackButtonClicked() {
        if (_fromPause) {
            // do nothing because pause screen will manage it 
        } else {
            Hide();
        }
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

    private void TryRevert() {
        // Did we save? 
        if (didSave) {
            // No need to rewert
            didSave = false;
        } else {
            fullscreenMode = Screen.fullScreenMode;
        }
    }

    internal void Show(bool fromPause) {
        _fromPause = fromPause;
        //_containerMain.SetActive(true);
        OnSettingChange?.Invoke(true);
    }
    internal void Hide() {
        TryRevert();
        //_containerMain.SetActive(false);
        OnSettingChange?.Invoke(false);
    }
}