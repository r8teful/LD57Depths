using System.Collections.Generic;
using UnityEngine;
public class WorldJuiceCreator : Singleton<WorldJuiceCreator> {
    [SerializeField] private ParticleSystem _critParticle;
    private Queue<ParticleSystem> _critQueue = new Queue<ParticleSystem>();

    protected override void Awake() {
        base.Awake();
        InitializeCritPool();
    }

    private void InitializeCritPool() {
        for (int i = 0; i < 4; i++) {
            CreateNewCritPoolObject();
        }
    }
    internal void SpawnCrit(Vector2 point) {
        if (_critQueue.Count == 0) {
            // Auto-expand pool if we run out
            CreateNewCritPoolObject();
        }
        ParticleSystem particles = _critQueue.Dequeue();
        particles.transform.position = point;
        // Activate
        particles.gameObject.SetActive(true);
        particles.Play(); // Will call ReturnToPool when finished
    }
    private void CreateNewCritPoolObject() {
        ParticleSystem particles = Instantiate(_critParticle, transform);
        particles.gameObject.SetActive(false);
        _critQueue.Enqueue(particles);
    }
    public void ReturnCritToPool(ParticleSystem p) {
        if (p == null) return;
        p.gameObject.SetActive(false);
        _critQueue.Enqueue(p);
    }
}