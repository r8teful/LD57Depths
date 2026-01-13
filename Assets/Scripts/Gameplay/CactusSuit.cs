using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class CactusSuit : MonoBehaviour, IInitializableAbility {
    private AbilityInstance _abilityInstance;
    private PlayerAbilities _owner;
    private NetworkedPlayer _player;
    private Coroutine _loop;

    [Header("Bullet Settings")]
    public GameObject cactusProjectile;      
    public int bulletsPerBurst = 8;
    public float angleRandomness = 5f;
    public float bulletSpeed = 8f;
    public float bulletLifetime = 5f;
    public void Init(AbilityInstance instance, NetworkedPlayer player) {
        _player = player;
        _abilityInstance = instance;
        _loop = StartCoroutine(FireLoop());
    }

    IEnumerator FireLoop() {
        while (true) {
            if (_abilityInstance == null) yield break; 
            float wait = _abilityInstance.GetEffectiveStat(StatType.Cooldown); // this clamps 
            Shoot();
            yield return new WaitForSeconds(wait);
        }
    }

    // The Shoot function the user requested
    void Shoot() {
        if (cactusProjectile == null) {
            Debug.LogWarning("Shooter: bulletPrefab is not assigned.");
            return;
        }

        int count = Mathf.Max(1, bulletsPerBurst);
        float angleStep = 360f / count;

        float burstRotation = Random.Range(0f, 360f);

        float[] angles = new float[count];
        for (int i = 0; i < count; i++) {
            float baseAngle = burstRotation + i * angleStep;
            float jitter = Random.Range(-angleRandomness, angleRandomness);
            angles[i] = baseAngle + jitter;
        }

        // Spawn bullets for each angle
        for (int i = 0; i < count; i++) {
            float a = angles[i];
            float rad = a * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;

            // Get a bullet instance (pooled or new)
            CactusProjectile prickle = null;
            GameObject go = Instantiate(cactusProjectile);
            prickle = go.GetComponent<CactusProjectile>();
            if (prickle == null) {
                Debug.LogError("bulletPrefab does not have a Bullet component.");
                Destroy(go);
                continue;
                
            }
            // Initialize and position the bullet
            prickle.transform.SetPositionAndRotation(transform.position, Quaternion.Euler(0f, 0f, a));
            prickle.gameObject.SetActive(true);
            prickle.Init(_player, dir, bulletSpeed, bulletLifetime);
        }
    }

    void OnDestroy() {
        if (_loop != null) StopCoroutine(_loop);
    }
}