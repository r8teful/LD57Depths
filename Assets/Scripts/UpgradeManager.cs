using System;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : StaticInstance<UpgradeManager> {

    private Dictionary<UpgradeType, int> upgradeLevels = new Dictionary<UpgradeType, int>();
    private Dictionary<UpgradeType, float> upgradeValues = new Dictionary<UpgradeType, float>();

    // Public only to see
    public Dictionary<Tile.TileType, int> playerResources = new Dictionary<Tile.TileType, int>();
    public static event Action UpgradeBought;

    [System.Serializable]
    public class UpgradeData {
        public UpgradeType type;
        public IncreaseType increaseType;
        public float baseValue;
        public float increasePerLevel;
        public UpgradeCostData[] costData; // Array to hold costs for different resource
    }

    public UpgradeData[] upgrades; // Assign in Unity Inspector

    [System.Serializable]
    public class UpgradeCostData {
        public Tile.TileType resourceType;
        public float baseCost;
        public float increasePerLevel;
        public IncreaseType increaseType;
        public int requiredAtLevel; // NEW: Only required at this level and beyond
    }


    protected override void Awake() {
        base.Awake();
        // Initialize upgrades
        foreach (UpgradeData upgrade in upgrades) {
            upgradeLevels[upgrade.type] = 0; // Start at level 0
            upgradeValues[upgrade.type] = upgrade.baseValue; // Set base value
        }
    }

    public void BuyUpgrade(UpgradeType type) {
        if (!upgradeLevels.ContainsKey(type)) return;
        UpgradeData upgrade = GetUpgradeData(type);
        if (upgrade == null) return;
        
        int currentLevel = upgradeLevels[type];
        Dictionary<Tile.TileType, int> upgradeCost = GetUpgradeCost(type);


        // Check if the player has enough resources
        foreach (var cost in upgradeCost) {
            if (!playerResources.ContainsKey(cost.Key) || playerResources[cost.Key] < cost.Value) {
                Debug.Log($"Not enough {cost.Key}! Need {cost.Value}, have {playerResources[cost.Key]}.");
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

        UpgradeBought?.Invoke();
        Debug.Log($"{type} upgraded to Level {upgradeLevels[type]}. New Value: {upgradeValues[type]}");
    }
    public float GetUpgradeValue(UpgradeType type) {
        return upgradeValues.ContainsKey(type) ? upgradeValues[type] : 0f;
    }

    public int GetUpgradeLevel(UpgradeType type) {
        return upgradeLevels.ContainsKey(type) ? upgradeLevels[type] : 0;
    }

    private UpgradeData GetUpgradeData(UpgradeType type) {
        foreach (UpgradeData upgrade in upgrades) {
            if (upgrade.type == type)
                return upgrade;
        }
        return null;
    }
    public Dictionary<Tile.TileType, int> GetUpgradeCost(UpgradeType type) {
        UpgradeData upgrade = GetUpgradeData(type);
        Dictionary<Tile.TileType, int> costDict = new Dictionary<Tile.TileType, int>();

        int currentLevel = upgradeLevels[type];

        foreach (var cost in upgrade.costData) {
            if (currentLevel < cost.requiredAtLevel)
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
    public void AddResource(Tile.TileType resource) {
        if (playerResources.ContainsKey(resource)) {
            playerResources[resource]++;
        } else {
            // Cool "new resource found" popup!? 
            playerResources.Add(resource, 1);
        }
    }
}

// Enum for different upgrade types
public enum UpgradeType {
    MiningSpeed,
    MiningDamange,
    MovementSpeed,
    OxygenCapacity,
    ResourceCapacity
}
public enum IncreaseType {
    Add,
    Multiply
}