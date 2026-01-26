using UnityEngine;
[RequireComponent (typeof(ParticleSystem))]
public class TrashParticleSystem : MonoBehaviour {
    private ParticleSystem _particles;
    private float _particleRateOverTime;
    private const float maxToRemoveAll = 50;
    private void Awake() {
        _particles = GetComponent<ParticleSystem>();
        _particleRateOverTime = _particles.emission.rateOverTime.constant;
    }
    private void OnEnable() {
    }

}
