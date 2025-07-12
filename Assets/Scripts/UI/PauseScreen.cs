using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseScreen : MonoBehaviour
{
    public AudioMixerGroup SFXMixer;
    public AudioMixerGroup MusicMixer;
    public Slider SFXSlider;
    public Slider MusicSlider;
    public void OnMenuClicked() {
        SceneManager.LoadScene(0);
    }
    public void OnRestartClicked() {
        SceneManager.LoadScene(1);
    }
    public void OnPauseCloseClicked() {
        Debug.LogWarning("No pause logic!");
        //UIMenuManager.Instance.OnPauseClose();
    }
    public void OnSFXChanged() {
        SFXMixer.audioMixer.SetFloat("sfx", Mathf.Log10(SFXSlider.value) * 20);
    }
    public void OnMuiscChanged() {
        SFXMixer.audioMixer.SetFloat("music", Mathf.Log10(MusicSlider.value) * 20);
    }
    public void Start() {
        if (SFXMixer.audioMixer.GetFloat("sfx", out float sfx)) {
            SFXSlider.value = Mathf.Pow(10, sfx / 20);
        }
        if (SFXMixer.audioMixer.GetFloat("music", out float music)) {
            MusicSlider.value = Mathf.Pow(10, music / 20);
        }
    }
}
