using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UpgradeManager : StaticInstance<UpgradeManager> {

    private Dictionary<UpgradeType, int> upgradeLevels = new Dictionary<UpgradeType, int>();
    private Dictionary<UpgradeType, float> upgradeValues = new Dictionary<UpgradeType, float>();

    // Public only to see
    public Dictionary<ItemData, int> playerResources = new Dictionary<ItemData, int>();
    public static event Action<UpgradeType> UpgradeBought;

    public UpgradeDataSO[] upgrades; // Assign in Unity Inspector


    public UIResourceElement resourceElement;
    public Transform ElementContainer;
    private List<UIResourceElement> _instantiatedUIVisual = new List<UIResourceElement>();

    protected override void Awake() {
        base.Awake();
        // Initialize upgrades
        foreach (UpgradeDataSO upgrade in upgrades) {
            upgradeLevels[upgrade.type] = 0; // Start at level 0
            upgradeValues[upgrade.type] = upgrade.baseValue; // Set base value
        }
        //playerResources.Add(Tile.TileType.Ore_Silver,99999);
    }
    public void DEBUGMAXResources() {
        for (int i = 0; i < 1000; i++) {
            
        //AddResource(TileScript.TileType.Ore_Stone);
        //AddResource(TileScript.TileType.Ore_Silver);
        //AddResource(TileScript.TileType.Ore_Ruby);
        //AddResource(TileScript.TileType.Ore_Gold);
        //AddResource(TileScript.TileType.Ore_Diamond);
        }
    }
    public void BuyUpgrade(UpgradeType type) {
        if (!upgradeLevels.ContainsKey(type)) return;
        UpgradeDataSO upgrade = GetUpgradeData(type);
        if (upgrade == null) return;
        
        int currentLevel = upgradeLevels[type];
        Dictionary<ItemData, int> upgradeCost = GetUpgradeCost(type);


        // Check if the player has enough resources
        foreach (var cost in upgradeCost) {
            if (!playerResources.ContainsKey(cost.Key) || playerResources[cost.Key] < cost.Value) {
                //Debug.Log($"Not enough {cost.Key}! Need {cost.Value}, have {playerResources[cost.Key]}.");
                return;
            }
        }
        
        // Deduct resources & apply upgrade
        foreach (var cost in upgradeCost) {
            playerResources[cost.Key] -= cost.Value;
        }

        upgradeLevels[type]++;
        if (upgrade.increaseType == IncreaseType.Add) {
            upgradeValues[type] += upgrade.increasePerLevel;
        } else if (upgrade.increaseType == IncreaseType.Multiply) {
            upgradeValues[type] *= (1 + upgrade.increasePerLevel);
        }

        UpgradeBought?.Invoke(type);
        UpdateResourceVisual();
        Debug.Log($"{type} upgraded to Level {upgradeLevels[type]}. New Value: {upgradeValues[type]}");
    }
    public bool HasEnoughResources(Dictionary<ItemData,int> costs) {
        foreach (var cost in costs) {
            if (!playerResources.ContainsKey(cost.Key) || playerResources[cost.Key] < cost.Value) {
                //Debug.Log($"Not enough {cost.Key}! Need {cost.Value}, have {playerResources[cost.Key]}.");
                return false;
            }
        }
        return true;
    }
    public bool TryRemoveResources(Dictionary<ItemData, int> resources, out bool b) {
        b = false;
        // Check first 
        if (HasEnoughResources(resources)) {
            b = true;
            foreach (var cost in resources) {
                playerResources[cost.Key] -= cost.Value; 
            }
            UpdateResourceVisual();
        } else {
            Debug.Log("NOT enough resources!");
        }
        return b;
    }
    private void UpdateResourceVisual() {
        foreach (var item in _instantiatedUIVisual) {
            item.Init(item.ResourceType, playerResources[item.ResourceType]);
        }
    }

    public float GetUpgradeValue(UpgradeType type) {
        return upgradeValues.ContainsKey(type) ? upgradeValues[type] : 0f;
    }

    public int GetUpgradeLevel(UpgradeType type) {
        return upgradeLevels.ContainsKey(type) ? upgradeLevels[type] : 0;
    }

    private UpgradeDataSO GetUpgradeData(UpgradeType type) {
        foreach (UpgradeDataSO upgrade in upgrades) {
            if (upgrade.type == type)
                return upgrade;
        }
        return null;
    }
    public Dictionary<ItemData, int> GetUpgradeCost(UpgradeType type) {
        UpgradeDataSO upgrade = GetUpgradeData(type);
        Dictionary<ItemData, int> costDict = new Dictionary<ItemData, int>();

        int currentLevel = upgradeLevels[type];

        foreach (var cost in upgrade.costData) {
            if (currentLevel < cost.requiredAtLevel || currentLevel >= cost.stopsAtLevel)
                continue; // Skip this resource if it isn't needed at this level
            float costValue = cost.baseCost;
            if (cost.increaseType == IncreaseType.Add) {
                costValue += cost.increasePerLevel * currentLevel;
            } else if (cost.increaseType == IncreaseType.Multiply) {
                costValue *= Mathf.Pow(1 + cost.increasePerLevel, currentLevel);
            }

            costDict[cost.resourceType] = Mathf.RoundToInt(costValue);
        }

        return costDict;
    }
    public void AddResource(ItemData resource) {
        Debug.Log("Adding resource" + resource);
        if (playerResources.ContainsKey(resource)) {
            playerResources[resource]++;
            foreach(var item in _instantiatedUIVisual) {
                if(item.ResourceType == resource) {
                    item.Init(resource, playerResources[resource]);
                }
            }
        } else {
            // Cool "new resource found" popup!? 
            playerResources.Add(resource, 1);
            var i = Instantiate(resourceElement, ElementContainer);
            i.Init(resource, playerResources[resource]);
            _instantiatedUIVisual.Add(i);
        }
       // UIInventoryManager.Instance.UpdateInventory(playerResources);
        // Pass playersources 
    }
    public void RemoveAllResources(float reductionAmount) {
        foreach(var r in playerResources.ToList()) {
            playerResources[r.Key] = Mathf.FloorToInt(r.Value * reductionAmount);
        }
        UpdateResourceVisual();
    }
}

// Enum for different upgrade types
public enum UpgradeType {
    MiningSpeed,
    MiningDamange,
    MovementSpeed,
    OxygenCapacity,
    ResourceCapacity,
    Light
}
public enum IncreaseType {
    Add,
    Multiply
}