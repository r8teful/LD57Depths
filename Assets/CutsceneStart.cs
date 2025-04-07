using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CutsceneStart : MonoBehaviour {
    public GameObject[] LoreFrames;
    public GameObject Slider;
    private SequenceSubGoingUp p;
    private void Start() {
        foreach(var g in LoreFrames) {
            g.SetActive(false);
        }
        StartCoroutine(ShowLore());
        GridManager.Instance.SetSlider(Slider.GetComponentInChildren<Slider>());
        Slider.SetActive(false); 
    }
    public IEnumerator ShowLore() {
        yield return new WaitForSeconds(1);
        LoreFrames[0].SetActive(true);
        yield return new WaitForSeconds(3);
        LoreFrames[1].SetActive(true);
        yield return new WaitForSeconds(3);
        LoreFrames[2].SetActive(true);
        yield return new WaitForSeconds(2.5f);
        LoreFrames[3].SetActive(true);
        yield return new WaitForSeconds(4);
        Slider.SetActive(true); // Show progress
        p.SetIntroDone();
    }

    internal void SetParent(SequenceSubGoingUp sequenceSubGoingUp) {
        p = sequenceSubGoingUp;
    }
}
