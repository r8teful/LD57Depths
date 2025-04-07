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
}
