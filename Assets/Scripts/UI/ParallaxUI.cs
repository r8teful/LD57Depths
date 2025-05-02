using UnityEngine;

public class ParallaxUI : MonoBehaviour {
    public Transform[] layers; // Assign your 6 depth images in order
    public float parallaxStrength = 10f; // Adjust this to control the effect strength

    private Vector3[] initialPositions;
    private Vector3 lastMousePosition;

    void Start() {
        initialPositions = new Vector3[layers.Length];
        for (int i = 0; i < layers.Length; i++) {
            initialPositions[i] = layers[i].position;
        }
        lastMousePosition = Input.mousePosition;
    }

    void Update() {
        Vector3 mouseDelta = (Input.mousePosition - lastMousePosition) * 0.001f; // Scale down effect
        lastMousePosition = Input.mousePosition;

        for (int i = 0; i < layers.Length; i++) {
            float depthFactor = (i + 1) / (float)layers.Length; // Closer layers move more
            Vector3 newPosition = initialPositions[i] + new Vector3(mouseDelta.x, mouseDelta.y, 0) * parallaxStrength * depthFactor;
            layers[i].position = Vector3.Lerp(layers[i].position, newPosition, Time.deltaTime * 5f);
        }
    }
}
