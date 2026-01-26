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

    private void OnTerraformChange(TerraformType type, float value) {
        if (type != TerraformType.Polution) return;
        if (value == 0) return;
        // Get total cleaned amount
        float particlesToSpawn = 1 - (maxToRemoveAll / value);
        var e = _particles.emission;
        e.rateOverTime = particlesToSpawn;
    }
}
