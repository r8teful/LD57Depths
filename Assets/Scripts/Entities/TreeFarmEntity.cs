using FishNet.Object;
using System;
using System.Collections.Generic;
using UnityEngine;

public class TreeFarmEntity : NetworkBehaviour, ITerraformContributor {
    private GrowableEntity _growComponent;
    public event Action OnFactorsChanged;
    private float _baseOxygen = 1; // This is the amount of oxygen the tree provides when fully grown
    private void Awake() {
        _growComponent = GetComponent<GrowableEntity>();
        if (_growComponent == null) {
            Debug.LogError("Can not find GrowableEntity on treeFarm!");
            return;
        }
        GrowableEntity.OnServerGrowthStageAdvanced += OnGrown;
    }

    private void OnGrown(GrowableEntity ent) {
        if (base.IsServerInitialized) {
            Debug.Log("tree grown on server!");
            if (_growComponent == ent) {
                // Match!
                OnFactorsChanged?.Invoke();
            }
        }
    }

    public List<TerraformFactor> GetTerraformFactors() {
        float total = _baseOxygen * ((float)_growComponent.GrowthStage / _growComponent.TotalStages);
        Debug.Log("calculated: " + total);
        return new List<TerraformFactor> {
        new(TerraformType.Oxygen, total)
        };
    }
}