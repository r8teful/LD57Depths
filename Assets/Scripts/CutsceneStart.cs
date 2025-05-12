using System.Collections;
using UnityEngine;

public class CutsceneStart : MonoBehaviour {
    public GameObject[] LoreFrames;
    public GameObject Slider;
    private void Start() {
        foreach(var g in LoreFrames) {
            g.SetActive(false);
        }
        StartCoroutine(ShowLore());
        Slider.SetActive(false); 
    }
    public IEnumerator ShowLore() {
        yield return new WaitForSeconds(1);
        LoreFrames[0].SetActive(true);
        AudioController.Instance.PlaySound2D("ClickTypeWriter", 0.8f, pitch: new AudioParams.Pitch(AudioParams.Pitch.Variation.Small));
        yield return new WaitForSeconds(3);
        LoreFrames[1].SetActive(true);
        AudioController.Instance.PlaySound2D("ClickTypeWriter", 0.8f, pitch: new AudioParams.Pitch(AudioParams.Pitch.Variation.Small));
        yield return new WaitForSeconds(3);
        LoreFrames[2].SetActive(true);
        AudioController.Instance.PlaySound2D("ClickTypeWriter", 0.8f, pitch: new AudioParams.Pitch(AudioParams.Pitch.Variation.Small));
        yield return new WaitForSeconds(2.5f);
        LoreFrames[3].SetActive(true);
        AudioController.Instance.PlaySound2D("ClickTypeWriter", 0.8f, pitch: new AudioParams.Pitch(AudioParams.Pitch.Variation.Small));
        yield return new WaitForSeconds(4);
        Slider.SetActive(true); // Show progress
    }

}
