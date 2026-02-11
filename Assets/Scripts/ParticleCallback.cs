using System.Collections;
using UnityEngine;

public class ParticleCallback : MonoBehaviour {
    public int type = 0; // 0 is crit, idk this could just become an enum
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
        WorldJuiceCreator.Instance.ReturnCritToPool(_particleSystem);
        
    }

}