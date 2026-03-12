using UnityEngine;
using UnityEngine.Events;

public class ParallaxUI : MonoBehaviour {
    public Transform[] layers; 
    public float parallaxStrength = 10f;
    public UnityEvent<bool> OnSettingChange;
    private Vector3[] initialPositions;
    private Vector3 lastMousePosition;
    private bool _shouldParallax;

    // from inspector
    public void SettingChange(bool isOpen) {
        _shouldParallax = !isOpen;
    }

    void Start() {
        initialPositions = new Vector3[layers.Length];
        for (int i = 0; i < layers.Length; i++) {
            initialPositions[i] = layers[i].position;
        }
        lastMousePosition = Input.mousePosition;
    }

    void Update() {
        if (!_shouldParallax) return;
        Vector3 mouseDelta = (Input.mousePosition - lastMousePosition) * 0.001f; // Scale down effect
        lastMousePosition = Input.mousePosition;

        for (int i = 0; i < layers.Length; i++) {
            float depthFactor = (i + 1) / (float)layers.Length; // Closer layers move more
            Vector3 newPosition = initialPositions[i] + new Vector3(mouseDelta.x, mouseDelta.y, 0) * parallaxStrength * depthFactor;
            layers[i].position = Vector3.Lerp(layers[i].position, newPosition, Time.deltaTime * 5f);
        }
    }
}
