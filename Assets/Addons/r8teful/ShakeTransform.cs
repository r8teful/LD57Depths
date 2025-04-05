using System.Collections;
using UnityEngine;
using DG.Tweening;
public class ShakeTransform : MonoBehaviour {
    [SerializeField] private AnimationCurve curve; 
    public void Shake(float duration, float magnitude) {
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }
    public void ShakeTween(float duration, float magnitude) {
        transform.DOShakePosition(duration, magnitude,randomnessMode: ShakeRandomnessMode.Harmonic);
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude) {
        Vector3 originalPos = transform.position;
        float elapsed = 0.0f;

        while (elapsed < duration) {
            float progress = elapsed / duration; 
            float currentMagnitude = magnitude * curve.Evaluate(progress);

            float x = originalPos.x + Random.Range(-1f, 1f) * currentMagnitude;
            float y = originalPos.y + Random.Range(-1f, 1f) * currentMagnitude;

            transform.position = new Vector3(x, y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPos;
    }
}