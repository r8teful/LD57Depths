using UnityEngine;
using UnityEngine.UI;

public class UISoundSettings : MonoBehaviour {
    AudioController _audio;
    [SerializeField] private Slider _masterSlider;
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Slider _ambienceSlider;
    private void Awake() {
        if (AudioController.Instance == null) {
            Debug.LogError("No Audio controller found we need it to control sound volume from settings!");
        }
        _audio = AudioController.Instance;
        if (_musicSlider == null || _sfxSlider == null || _ambienceSlider == null) {
            Debug.LogError("Need to set sliders in inspector!");
        }
        _sfxSlider.onValueChanged.AddListener(OnSFXChanged);
        _musicSlider.onValueChanged.AddListener(OnMusicChanged);
        _ambienceSlider.onValueChanged.AddListener(OnAmbienceChanged);
        _masterSlider.onValueChanged.AddListener(OnMasterChanged);
    }
    public void OnSFXChanged(float newValue) {
        _audio.OnSfxVolumeChange(newValue);
    }
    public void OnMusicChanged(float newValue) {
        _audio.OnMusicVolumeChange(newValue);
    }
    public void OnAmbienceChanged(float newValue) {
        _audio.OnAmbienceVolumeChange(newValue);
    }
    public void OnMasterChanged(float newValue) {
        _audio.OnMasterVolumeChange(newValue);
    }
    
    public void Start() {
        if (_audio.TryGetSfxVolume(out float sfx)) {
            _musicSlider.value = Mathf.Pow(10, sfx / 20);
        }
        if (_audio.TryGetMusicVolume(out float music)) {
            _sfxSlider.value = Mathf.Pow(10, music / 20);
        }
        if (_audio.TryGetAmbienceVolume(out float ambience)) {
            _ambienceSlider.value = Mathf.Pow(10, ambience / 20);
        }
        if (_audio.TryGetMasterVolume(out float master)) {
            _masterSlider.value = Mathf.Pow(10, master / 20);
        }
    }
}
