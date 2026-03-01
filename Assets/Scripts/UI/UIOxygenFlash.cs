using System;
using System.Collections;
using UnityEngine;

public class UIOxygenFlash : MonoBehaviour {
    private bool _isFlashing;
    private Coroutine _flashCoroutine;
    public GameObject OxygenWarning;

    private void Awake() {
        OxygenManager.OnFlashStart += FlashStart;
        OxygenManager.OnFlashStop += FlashStop;
    }
    private void OnDestroy() {
        OxygenManager.OnFlashStart -= FlashStart;
        OxygenManager.OnFlashStop -= FlashStop;

    }
    private void Start() {
        OxygenWarning.SetActive(false);
    }
    private void FlashStop() {
        SliderFlash(false);
    }

    private void FlashStart() {
        SliderFlash(true);
    }

    public void SliderFlash(bool shouldFlash) {
        if (shouldFlash) {
            if (!_isFlashing) // Don't start a new coroutine if already flashing
            {
                _isFlashing = true;
                _flashCoroutine = StartCoroutine(FlashCoroutine());
            }
        } else {
            if (_isFlashing) // Only stop if currently flashing
            {
                _isFlashing = false;
                StopCoroutine(_flashCoroutine);
                OxygenWarning.SetActive(false); // Ensure it's visible when stopping the flash
            }
        }
    }
    private IEnumerator FlashCoroutine() {
        while (_isFlashing) {
            // Toggle the active state of the GameObject
            OxygenWarning.SetActive(!OxygenWarning.activeSelf);

            // Wait for the flashSpeed duration
            yield return new WaitForSeconds(0.2f);
        }
    }

}