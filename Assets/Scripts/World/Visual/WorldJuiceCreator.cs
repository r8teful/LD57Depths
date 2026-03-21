using System;
using System.Collections.Generic;
using UnityEngine;
public enum JuiceType {
    crit,
    explosion
}
public class WorldJuiceCreator : Singleton<WorldJuiceCreator> {
    [SerializeField] private ParticleSystem _critParticle;
    [SerializeField] private ParticleSystem _explosionParticle;
    private Queue<ParticleSystem> _critQueue = new Queue<ParticleSystem>();
    private Queue<ParticleSystem> _explosionQueue = new Queue<ParticleSystem>();

    protected override void Awake() {
        base.Awake(); 
        InitializePool(_critQueue, _critParticle, 4);
        InitializePool(_explosionQueue, _explosionParticle, 4);

    }
    internal void SpawnCrit(Vector2 point) => SpawnFromPool(_critQueue, _critParticle, point);
    internal void SpawnExplosion(Vector2 point) => SpawnFromPool(_explosionQueue, _explosionParticle, point);
    internal void ReturnParticleToPool(ParticleSystem p, JuiceType type) {
        switch (type) {
            case JuiceType.crit:
                ReturnToPool(_critQueue, p);
                break;
            case JuiceType.explosion:
                ReturnToPool(_explosionQueue, p);
                break;
            default:
                break;
        }
    }

    private void InitializePool(Queue<ParticleSystem> pool, ParticleSystem prefab, int size) {
        for (int i = 0; i < size; i++)
            CreateNewPoolObject(pool, prefab);
    }

    private void CreateNewPoolObject(Queue<ParticleSystem> pool, ParticleSystem prefab) {
        ParticleSystem p = Instantiate(prefab, transform);
        p.gameObject.SetActive(false);
        pool.Enqueue(p);
    }

    private ParticleSystem SpawnFromPool(Queue<ParticleSystem> pool, ParticleSystem prefab, Vector2 point) {
        if (pool.Count == 0)
            CreateNewPoolObject(pool, prefab);

        ParticleSystem p = pool.Dequeue();
        p.transform.position = point;
        p.gameObject.SetActive(true);
        p.Play();
        return p;
    }

    private void ReturnToPool(Queue<ParticleSystem> pool, ParticleSystem p) {
        if (p == null) return;
        p.gameObject.SetActive(false);
        pool.Enqueue(p);
    }


}