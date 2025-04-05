using Pixelplacement;
using UnityEngine;
using static Pixelplacement.Tween;

[RequireComponent(typeof(Light))]

public class FlickeringLight : MonoBehaviour {
    private Light _targetLight;

    [SerializeField] private float _minIntensity = 1f;
    [SerializeField] private float _maxIntensity = 2f;
    [SerializeField] private float _flickerSpeed = 2f;
    [SerializeField] private float _smoothTransitionSpeed = 1f;

    public enum LightMode {
        Flicker,
        SmoothTransition
    }

    [SerializeField] private LightMode _currentMode = LightMode.Flicker;

    private float baseIntensity;
    private float flickerTimer;

    void Start() {
        if (_targetLight == null) {
            _targetLight = GetComponent<Light>();
        }

        baseIntensity = _targetLight.intensity;

        if (_currentMode == LightMode.SmoothTransition) {
            StartSmoothTransition();
        }
    }

    void Update() {
        switch (_currentMode) {
            case LightMode.Flicker:
                FlickerUpdate();
                break;
            case LightMode.SmoothTransition:
                break;
                // Smooth transition is handled by the coroutine
        }
    }

    private void FlickerUpdate() {
        flickerTimer -= Time.deltaTime;
        if (flickerTimer <= 0f) {
            float newIntensity = Random.Range(_minIntensity, _maxIntensity);
            _targetLight.intensity = newIntensity;
            flickerTimer = 1f / _flickerSpeed;
        }
    }

    private void StartSmoothTransition() {
        //StopAllCoroutines();  // Stop any other running coroutines if necessary
        //StartCoroutine(SmoothTransition());
        LightIntensity(_targetLight,_maxIntensity,_smoothTransitionSpeed,0,Tween.EaseLinear,LoopType.PingPong);
    }
}
