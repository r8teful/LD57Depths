using System.Collections;
using UnityEngine;
[RequireComponent(typeof(ParticleSystem))]
public class ParticleAttractor : MonoBehaviour {
	private ParticleSystem _particleSystem;
	private ParticleSystem.Particle[] m_Particles;
	public Transform target;
	public float speed = 5f;
	public float delay = 5f;
	int numParticlesAlive;
    private bool _shouldStart;

    void Start () {
		_particleSystem = GetComponent<ParticleSystem>();
		if (!GetComponent<Transform>()){
			GetComponent<Transform>();
		}
	}

	public void StartAttract(Transform t) {
		target = t;
		_shouldStart = true;
    }

	void Update () {
        if (!_shouldStart) return;
		m_Particles = new ParticleSystem.Particle[_particleSystem.main.maxParticles];
		numParticlesAlive = _particleSystem.GetParticles(m_Particles);
		float step = speed * Time.deltaTime;
		for (int i = 0; i < numParticlesAlive; i++) {

            ref var p = ref m_Particles[i];
            float age = p.startLifetime - p.remainingLifetime;
            if (age < delay)
                continue;
            m_Particles[i].position = Vector3.LerpUnclamped(m_Particles[i].position, target.position, step);
		}
		_particleSystem.SetParticles(m_Particles, numParticlesAlive);
	}
}
