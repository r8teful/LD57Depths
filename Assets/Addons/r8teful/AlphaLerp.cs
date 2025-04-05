using System.Collections;
using UnityEngine;
using UnityEngine.UI;
public class AlphaLerp : MonoBehaviour {
    private RawImage rawImage;
    [SerializeField] private bool _blackToNothing;

    void Start() {
        rawImage = GetComponent<RawImage>();
        rawImage.color = new Color(rawImage.color.r, rawImage.color.g, rawImage.color.b, _blackToNothing ? 1f : 0f);
    }
    private void OnEnable() {
        rawImage = GetComponent<RawImage>();
    }

    public void SetInteractions(bool b) {
        rawImage.raycastTarget = !b;
    }

    public IEnumerator Fade(bool BlackToNothing, float duration = 1f) {
        float startAlpha = BlackToNothing ? 1f : 0f;
        float endAlpha = BlackToNothing ? 0f : 1f;
        rawImage.color = new Color(rawImage.color.r, rawImage.color.g, rawImage.color.b, startAlpha);
        float elapsedTime = 0f;

        while (elapsedTime < duration) {
            elapsedTime += Time.deltaTime;
            float lerpValue = Mathf.Clamp01(elapsedTime / duration);
            float alpha = Mathf.Lerp(startAlpha, endAlpha, lerpValue);

            rawImage.color = new Color(rawImage.color.r, rawImage.color.g, rawImage.color.b, alpha);
            yield return null;
        }
    }
}