using UnityEngine;

public class ParticleSpawnOnEvent : MonoBehaviour {
    [SerializeField] private ParticleSystem _system;
    // Called through the animation clip, name set in aseprite file
    private void OnParticle() {
        _system.Play();
    }
}