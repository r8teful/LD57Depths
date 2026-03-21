using UnityEngine;

public class ParticleCallback : MonoBehaviour {
    public JuiceType particleJuiceType; 
    private ParticleSystem _particleSystem;

    private void Awake() {
        _particleSystem = GetComponent<ParticleSystem>();
    }
    private void OnParticleSystemStopped() {
        if (WorldJuiceCreator.Instance == null) return;
        if (_particleSystem == null) { 
            _particleSystem = GetComponent<ParticleSystem>();
            if (_particleSystem == null) return;
        }
        WorldJuiceCreator.Instance.ReturnParticleToPool(_particleSystem,particleJuiceType);
    }

}