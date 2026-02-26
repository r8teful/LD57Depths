using UnityEngine;
using UnityEngine.UI;

public class UISoundSettings : MonoBehaviour {
    AudioController _audio;
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Slider _sfxSlider;
    private void Awake() {
        if (AudioController.Instance == null) {
            Debug.LogError("No Audio controller found we need it to control sound volume from settings!");
        }
        _audio = AudioController.Instance;
        if (_musicSlider == null || _sfxSlider == null) {
            Debug.LogError("Need to set sliders in inspector!");
        }
        _musicSlider.onValueChanged.AddListener(OnSFXChanged);
    }
    public void OnSFXChanged(float newValue) {
        _audio.OnSfxVolumeChange(newValue);
    }
    public void OnMuiscChanged(float newValue) {
        _audio.OnSfxVolumeChange(newValue);
    }
    public void Start() {
        if (_audio.TryGetSfxVolume(out float sfx)) {
            _musicSlider.value = Mathf.Pow(10, sfx / 20);
        }
        if (_audio.TryGetMusicVolume(out float music)) {
            _sfxSlider.value = Mathf.Pow(10, music / 20);
        }
    }
}
