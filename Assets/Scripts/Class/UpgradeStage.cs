using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// Helper struct for required items
[System.Serializable]
public struct ItemQuantity {
    public ItemData item;
    public int quantity;
    public ItemQuantity(ItemData item, int q) {
        this.item = item;
        quantity = q;
    }
    public ItemQuantity(ushort id, int q) {
        this.item = App.ResourceSystem.GetItemByID(id);
        quantity = q;
    }
}
[System.Serializable]
public struct IDQuantity {
    public ushort itemID;
    public int quantity;
    public IDQuantity(ushort id, int q) {
        itemID = id;
        quantity = q;
    }
}
// Helper struct for UI status
public struct IngredientStatus {
    public ItemData Item { get; }
    public int RequiredAmount { get; }
    public int CurrentAmount { get; }
    public bool HasEnough => CurrentAmount >= RequiredAmount;

    public IngredientStatus(ItemData item, int requiredAmount, int currentAmount) {
        Item = item;
        RequiredAmount = requiredAmount;
        CurrentAmount = currentAmount;
    }
}
public struct NodeProgressionStatus {
    public int LevelMax { get; }
    public int LevelCurr { get; }
    public readonly bool ShouldShow => LevelMax > 1;
    public NodeProgressionStatus(int levelMax, int levelCurr) {
        LevelMax = levelMax;
        LevelCurr = levelCurr;
    }
}
[Serializable]
public class UpgradeStage : IExecutable {
    [Tooltip("The Scriptable Object defining the upgrade for this level.")]
    [InlineEditor]
    [GUIColor("#cbebca")]
    [SerializeReference]
    public List<UpgradeEffect> effects = new List<UpgradeEffect>(); // The results the upgrade has when purchased 
  
    [Range(0.1f, 5f)]
    public float costMultiplier = 1.0f;
    public int costTier;
    public UpgradeTierSO upgradeItemPool;
    public UpgradeStageExtraDataSO extraData;

    public void Execute(ExecutionContext context) {
        foreach (var effect in effects) {
            effect.Execute(context);
        }
    }
    public List<StatChangeStatus> GetStatStatuses() {
        var statuses = new List<StatChangeStatus>();
        foreach (var effect in effects) {
            statuses.Add(effect.GetChangeStatus());
        }
        return statuses;
    }
}