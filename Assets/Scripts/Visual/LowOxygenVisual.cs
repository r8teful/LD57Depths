using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// This gets spawned as soon as the player is low enough on oxygen
public class LowOxygenVisual : MonoBehaviour {
    public float rampUpDuration = 10f;        // normal ramp in time
    public float rampDownDuration = 0.4f;    // faster ramp out on cancel


    [Header("Effect Targets (applied in profile)")]
    [Range(0f, 1f)] public float targetVignetteIntensity = 0.8f;
    [Range(-100f, 0f)] public float targetSaturation = -60f; // negative = desaturate
    [Range(0f, 1f)] public float targetChromatic = 0.8f;
    [Range(0f, 1f)] public float targetFilmGrain = 0.25f;

 
    [Header("Optional")]
    [Tooltip("If provided, the script will reuse this profile. Otherwise a runtime profile will be created.")]
    public VolumeProfile presetProfile;

    // Internal
    Volume _volume;
    VolumeProfile _runtimeProfile;
    bool _isCancelling = false;
    Coroutine _activeCoroutine;

    void Awake() {
        CreateGlobalVolume();
    }

    void OnDestroy() {
        // Clean up profile we created at runtime to avoid leaking ScriptableObjects
        if (_runtimeProfile != null) {
            // DestroyImmediate to remove ScriptableObject assets created at runtime
#if UNITY_EDITOR
            DestroyImmediate(_runtimeProfile);
#else
            Destroy(_runtimeProfile);
#endif
            _runtimeProfile = null;
        }
    }

    void CreateGlobalVolume() {
        // Ensure GameObject exists with Volume
        _volume = gameObject.AddComponent<Volume>();
        _volume.isGlobal = true;
        _volume.priority = 1000f; // high priority so it definitely blends in
        _volume.weight = 0f; // start silent

        // Use preset profile or create runtime profile with our overrides
        if (presetProfile != null) {
            _volume.profile = presetProfile;
        } else {
            _runtimeProfile = ScriptableObject.CreateInstance<VolumeProfile>();

            // Create and configure overrides
            var vignette = ScriptableObject.CreateInstance<Vignette>();
            vignette.intensity.overrideState = true;
            vignette.intensity.value = targetVignetteIntensity;
            vignette.smoothness.overrideState = true;
            vignette.smoothness.value = 0.6f;
            vignette.active = true;
            _runtimeProfile.components.Add(vignette);

            var color = ScriptableObject.CreateInstance<ColorAdjustments>();
            color.saturation.overrideState = true;
            color.saturation.value = targetSaturation;
            color.active = true;
            _runtimeProfile.components.Add(color);

            var chroma = ScriptableObject.CreateInstance<ChromaticAberration>();
            chroma.intensity.overrideState = true;
            chroma.intensity.value = targetChromatic;
            chroma.active = true;
            _runtimeProfile.components.Add(chroma);

            var grain = ScriptableObject.CreateInstance<FilmGrain>();
            grain.intensity.overrideState = true;
            grain.intensity.value = targetFilmGrain;
            grain.response.overrideState = true;
            grain.response.value = 1f;
            grain.active = true;
            _runtimeProfile.components.Add(grain);

            _volume.profile = _runtimeProfile;
        }
    }

    public void Play() {
        if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
        _isCancelling = false;
        _activeCoroutine = StartCoroutine(RampVolumeWeight(0f, 1f, rampUpDuration, new(), onComplete: null));
    }
    void Start() {
        var vignette = ScriptableObject.CreateInstance<Vignette>();
        vignette.active = true;
        vignette.intensity.Override(1f);
        VolumeProfile v;
        /*
        volume.weight = 0f;
        DOTween.Sequence()
           .Append(DOTween.To(() => volume.weight, x => volume.weight = x, 1f, 1f))
           .AppendInterval(1f)
           .Append(DOTween.To(() => volume.weight, x => volume.weight = x, 0f, 1f))
           .OnComplete(() => {
               RuntimeUtilities.DestroyVolume(volume, true, true);
               Destroy(this);
           });
         */
    }

    public void CancelAndRemove() {
        if (_isCancelling) return;
        _isCancelling = true;

        // stop any existing ramp coroutine and start ramp down from current weight
        if (_activeCoroutine != null) StopCoroutine(_activeCoroutine);
        float startWeight = _volume != null ? _volume.weight : 0f;
        AnimationCurve curve = new();
        _activeCoroutine = StartCoroutine(RampVolumeWeight(startWeight, 0f, rampDownDuration, curve, onComplete: () => {
            // cleanup: destroy object
            if (this != null) Destroy(gameObject);
        }));
    }

    IEnumerator RampVolumeWeight(float from, float to, float duration, AnimationCurve curve, System.Action onComplete) {
        if (_volume == null) yield break;
        float elapsed = 0f;
        // if duration is effectively zero, snap
        if (duration <= 0.0001f) {
            _volume.weight = to;
            onComplete?.Invoke();
            yield break;
        }

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float curveT = curve != null ? curve.Evaluate(t) : t;
            _volume.weight = Mathf.LerpUnclamped(from, to, curveT);
            yield return null;
        }

        _volume.weight = to;
        onComplete?.Invoke();
    }


}