using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;

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
    private Dictionary<TerraformType, float> _terraformingAmountsCurrent = new Dictionary<TerraformType, float>();
    private Dictionary<TerraformType, float> _terraformingAmountsMax = new Dictionary<TerraformType, float>();
    private HashSet<ITerraformContributor> _contributors = new HashSet<ITerraformContributor>();
    public event Action<TerraformType, float> OnTotalChanged;

    public void AddPollutionCleaned(float amount) {
        if (amount <= 0) return;
        CurrentPollutionCleaned.Value += amount;
    }

    private void RecalculateAll() {
        _terraformingAmountsCurrent.Clear();
        _terraformingAmountsMax.Clear();
        foreach (var c in _contributors) { 
            AddFactors(c.GetTerraformFactors());
            AddFactorsMax(c.GetTerraformFactorsMax());
        }
        // notify changes for all types (optional)
        foreach (var kv in _terraformingAmountsCurrent) OnTotalChanged?.Invoke(kv.Key, kv.Value);
    }


    private void AddFactors(List<TerraformFactor> factors) {
        if (factors == null) return;
        foreach (var f in factors) {
            if (_terraformingAmountsCurrent.ContainsKey(f.Type)) _terraformingAmountsCurrent[f.Type] += f.Amount;
            else _terraformingAmountsCurrent[f.Type] = f.Amount;
        }
    }
    private void AddFactorsMax(List<TerraformFactor> factors) {
        if (factors == null) return;
        foreach (var f in factors) {
            if (_terraformingAmountsMax.ContainsKey(f.Type)) _terraformingAmountsMax[f.Type] += f.Amount;
            else _terraformingAmountsMax[f.Type] = f.Amount;
        }
    }

    public float GetTotal(TerraformType type) {
        if (_terraformingAmountsCurrent.TryGetValue(type, out var v)) return v;
        return 0f;
    }
    public float GetMaxPotential(TerraformType type) {
        if (_terraformingAmountsMax.TryGetValue(type, out var v)) return v;
        return 0f;
    }

    internal void DEBUGSetValue(float v) {
        if (_terraformingAmountsCurrent.ContainsKey(TerraformType.Oxygen)) {
            _terraformingAmountsCurrent[TerraformType.Oxygen] = v;
            OnTotalChanged?.Invoke(TerraformType.Oxygen, v);
        }
    }
}
