using UnityEngine;

public class ParticleDestroyOnFinish : MonoBehaviour {
    private ParticleSystem[] _particleSystems;

    private void Awake() {
        // Find all particle systems on this object and its children
        _particleSystems = GetComponentsInChildren<ParticleSystem>(false);
    }

    private void Update() {
        // If there are no particle systems, just destroy immediately
        if (_particleSystems == null || _particleSystems.Length == 0) {
            Destroy(gameObject);
            return;
        }

        // Check if all particle systems have finished
        bool allDone = true;
        foreach (var ps in _particleSystems) {
            if (ps == null) continue;

            // Check if the particle system is still alive (either emitting or particles still exist)
            if (ps.IsAlive(true)) {
                allDone = false;
                break;
            }
        }

        if (allDone) {
            Destroy(gameObject);
        }
    }
}
