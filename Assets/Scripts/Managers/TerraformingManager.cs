// TerraformingManager.cs
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton manager to track all terraforming stats for the planet.
/// Provides public methods to modify stats and an event to notify other systems of changes.
/// </summary>
public class TerraformingManager : NetworkBehaviour {
    public static TerraformingManager Instance { get; private set; }
    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject); // Handle duplicates
        } else {
            Instance = this;
            GrowableEntity.OnServerGrowthStageAdvanced += OnEntityGrow;
            EntityManager.EntitySpawnedNew += OnEntitySpawned;
            EntityManager.EntityDepawnedPermanent+= OnEntityDespawned;
        }
    }
    private void OnEntitySpawned(PersistentEntityData data) {
        if(data==null) return;
        if(data.activeInstance==null) return;
        if(data.activeInstance.TryGetComponent<TerraformEntity>(out var terra)) {
            _entities.Add(terra); // Now this includes every entity that has that component
        }
    }
    private void OnEntityDespawned(PersistentEntityData data) {
        if (data == null) return;
        if (data.activeInstance == null) return;
        if (data.activeInstance.TryGetComponent<TerraformEntity>(out var terra)) {
            if(_entities.Contains(terra)) 
                _entities.Remove(terra);
        }
    }

    private void OnEntityGrow(GrowableEntity entity) {
        if (entity == null)
            return;
        // TODO
    }
    private readonly SyncVar<float> _currentLight = new SyncVar<float>();
    private readonly SyncVar<float> _currentOxygen = new SyncVar<float>();
    private readonly SyncVar<float> _currentPollutionCleaned = new SyncVar<float>();
    public SyncVar<float> CurrentOxygen => _currentOxygen;
    public SyncVar<float> CurrentPollutionCleaned => _currentPollutionCleaned;
    public SyncVar<float> CurrentLight => _currentLight;

    private List<TerraformEntity> _entities;
    private HashSet<ulong> persistentLightEntities = new HashSet<ulong>();
    private HashSet<ulong> persistentOxygenEntities = new HashSet<ulong>();

    public void AddPollutionCleaned(float amount) {
        if (amount <= 0) return;
        CurrentPollutionCleaned.Value += amount;
    }

    [ServerRpc(RequireOwnership = false)]
    public void RegisterLight(ulong persistantID) {
        if (!persistentLightEntities.Contains(persistantID)) {
            persistentLightEntities.Add(persistantID);
        }
        RecalculateTotalLight();
    }
    [ServerRpc(RequireOwnership = false)]
    public void RegisterOxygen(ulong persistantID) {
        if(!persistentOxygenEntities.Contains(persistantID)){
            persistentOxygenEntities.Add(persistantID);
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void UnregisterLight(ulong persistantID) {
        if (!persistentLightEntities.Contains(persistantID)) {
            persistentLightEntities.Remove(persistantID);
        }
        RecalculateTotalLight();
    }
    [ServerRpc(RequireOwnership = false)]
    public void UnregisterOxygen(ulong persistantID) {
        if (!persistentOxygenEntities.Contains(persistantID)) {
            persistentOxygenEntities.Remove(persistantID);
        }
    }

    private void RecalculateTotalLight() {
        float totalLight = 0f;
        foreach (var emitter in persistentLightEntities) {
            PersistentEntityData p =  EntityManager.Instance.GetPersistantDataByID(emitter);
            if (p == null) {
                Debug.LogError("returned persistant data is null!");
                return;
            }
            if (p.specificData is LightEntityData light) {
                totalLight += light.LightLevel;
            } else {
                Debug.LogError("A registered persistant light entity does not have LightEntityData!");
            }
        }
        _currentLight.Value = totalLight;
    }
    private void RecalculateTotalLight2() {
        float totalLight = 0f;
        foreach (var emittor in _entities) {
            if (emittor.TryGetComponent<Light>(out var l)) {
                totalLight += l.intensity;
            }
        }
        _currentLight.Value = totalLight;
    }
}