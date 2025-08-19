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
            EntityManager.EntitySpawnedNew += OnEntitySpawned;
            EntityManager.EntityDepawnedPermanent+= OnEntityDespawned;
        }
    }
    private void OnEntitySpawned(PersistentEntityData data) {
        if(data==null) return;
        if(data.activeInstance==null) return;
        if(data.activeInstance.TryGetComponent<ITerraformContributor>(out var terra)) {
            _contributors.Add(terra); // Now this includes every entity that has that component
            terra.OnFactorsChanged += RecalculateAll;
            RecalculateAll();
        }
    }
    private void OnEntityDespawned(PersistentEntityData data) {
        if (data == null) return;
        if (data.activeInstance == null) return;
        if (data.activeInstance.TryGetComponent<ITerraformContributor>(out var terra)) {
            if (_contributors.Contains(terra)) {
                _contributors.Remove(terra);
                terra.OnFactorsChanged -= RecalculateAll;
                RecalculateAll();
            }
        }
    }

    private readonly SyncVar<float> _currentLight = new SyncVar<float>();
    private readonly SyncVar<float> _currentOxygen = new SyncVar<float>();
    private readonly SyncVar<float> _currentPollutionCleaned = new SyncVar<float>();
    public SyncVar<float> CurrentOxygen => _currentOxygen;
    public SyncVar<float> CurrentPollutionCleaned => _currentPollutionCleaned;
    public SyncVar<float> CurrentLight => _currentLight;

    // totals by type
    private readonly Dictionary<TerraformType, float> _totals = new Dictionary<TerraformType, float>();
    private readonly HashSet<ITerraformContributor> _contributors = new HashSet<ITerraformContributor>();
    public event Action<TerraformType, float> OnTotalChanged;

    public void AddPollutionCleaned(float amount) {
        if (amount <= 0) return;
        CurrentPollutionCleaned.Value += amount;
    }

    private void RecalculateAll() {
        _totals.Clear();
        foreach (var c in _contributors) AddFactors(c.GetTerraformFactors());
        // notify changes for all types (optional)
        foreach (var kv in _totals) OnTotalChanged?.Invoke(kv.Key, kv.Value);
    }


    private void AddFactors(List<TerraformFactor> factors) {
        if (factors == null) return;
        foreach (var f in factors) {
            if (_totals.ContainsKey(f.Type)) _totals[f.Type] += f.Amount;
            else _totals[f.Type] = f.Amount;
        }
    }

    public float GetTotal(TerraformType type) {
        if (_totals.TryGetValue(type, out var v)) return v;
        return 0f;
    }
}
