// TerraformingManager.cs
using FishNet.Object;
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
            EntityManager.EntitySpawnedNew += OnEntitySpawned;
            EntityManager.EntityDepawnedPermanent+= OnEntityDespawned;
        }
    }

    private void OnEntitySpawned(ulong arg1, PersistentEntityData arg2) {
        if (arg2 != null) {
            if (arg2.specificData is LightEntityData) {
                Debug.Log("Light EntitySpawned!" + arg2.cellPos);
            } else if (arg2.specificData is OxygenEntityData) { 
                Debug.Log("Oxygen EntitySpawned!" + arg2.cellPos);
            }
        }
    }
    private void OnEntityDespawned(ulong arg1, PersistentEntityData arg2) {
        Debug.Log("EntityDespawned!" + arg2.cellPos);
    }

    public float CurrentOxygen { get; private set; }
    public float CurrentPollutionCleaned { get; private set; }
    public float CurrentLight { get; private set; }

    public static event Action OnTerraformingStatsChanged;
    private HashSet<ulong> persistentLightEntities = new HashSet<ulong>();
    private HashSet<ulong> persistentOxygenEntities = new HashSet<ulong>();

    // --- Public Helper Functions (The API for other scripts) ---

    /// <summary>
    /// Adds a specified amount to the total pollution cleaned value.
    /// Typically called when a PollutionEntity is destroyed.
    /// </summary>
    public void AddPollutionCleaned(float amount) {
        if (amount <= 0) return;
        CurrentPollutionCleaned += amount;
        OnTerraformingStatsChanged?.Invoke();
    }

    [ServerRpc(RequireOwnership = false)]
    public void RegisterLight(ulong persistantID) {
        persistentLightEntities.Add(persistantID);
        RecalculateTotalLight();
    }
    [ServerRpc(RequireOwnership = false)]
    public void RegisterOxygen(ulong persistantID) {

    }
    [Server]
    private void RegisterTerraformMachine(ulong persistantID) {
        // TODO possibly call this from the start method of a machine, or handle it through the entity manager
        
    }
    [Server]
    private void UnregisterTerraformMachine(ulong persistantID) {
      
    }

    // --- Private Logic ---

    /// <summary>
    /// Iterates through all registered light emitters and sums their light values.
    /// </summary>
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
        CurrentLight = totalLight;
        OnTerraformingStatsChanged?.Invoke();
    }
}