using System.Collections;
using UnityEngine;

public class Vibration : MonoBehaviour {
    private float vibrationIntensity = 0.05f; // Adjust the intensity of the vibration
    private float vibrationSpeed = 500f; // Adjust the speed of the vibration
    private float vibrationDuration = 1f; // Adjust the duration of each vibration in seconds
    private float pauseDuration = 1f; // Adjust the duration of the pause between vibrations in seconds

    private Vector3 initialPosition;
    private bool isVibrating = false;

    public void StartVibration(Vector3 localpos) {
        if (!isVibrating) {
            isVibrating = true;
            initialPosition = localpos;
            StartCoroutine(VibrationLoop());
        }
    }

    private IEnumerator VibrationLoop() {
        while (isVibrating) {
            float startTime = Time.time;

            // Continue vibrating for the specified duration
            while (Time.time - startTime < vibrationDuration) {
                // Simulate vibration by moving the object back and forth along the Y-axis
                float vibrationOffset = Mathf.Sin(Time.time * vibrationSpeed) * 2 * vibrationIntensity;
                transform.localPosition = initialPosition + new Vector3(0f, vibrationOffset, 0f);

                yield return null;
            }

            //Reset position after each vibration duration
            transform.localPosition = initialPosition;

            // Check if isVibrating is still true (it might have been set to false during the vibration)
            if (isVibrating) {
                // Wait for the specified pause duration
                yield return new WaitForSeconds(pauseDuration);
            }
        }
    }
}
