using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class UpgradeStage {
    [Tooltip("The Scriptable Object defining the upgrade for this level.")]
    [InlineEditor]
    [GUIColor("#cbebca")]
    public UpgradeRecipeSO upgrade;

    [Range(0.1f, 5f)]
    public float costMultiplier = 1.0f;

    public int costTier;
    public UpgradeTierSO upgradeItemPool;
}

[CreateAssetMenu(fileName = "UpgradeNodeSO", menuName = "ScriptableObjects/Upgrades/UpgradeNodeSO")]
public class UpgradeNodeSO : ScriptableObject, IIdentifiable {
    [BoxGroup("Identification")]
    [HorizontalGroup("Identification/Left")]
    [VerticalGroup("Identification/Left/2")]
    public string nodeName;
    [VerticalGroup("Identification/Left/2")]
    [SerializeField] private ushort uniqueID;
    [VerticalGroup("Identification/Left/2")]
    public string description;
    [VerticalGroup("Identification/Left/1")]
    [PreviewField(75), HideLabel,LabelWidth(0)]
    public Sprite icon;

    [Tooltip("ANY of these prerequisite nodes must be fully unlocked before this one can be started.")]
    public List<UpgradeNodeSO> prerequisiteNodesAny;
    public bool UnlockedAtFirstPrereqStage;
    public List<UpgradeStage> stages =   new List<UpgradeStage>();
    public int MaxLevel => stages.Count;

    public ushort ID => uniqueID;

    public bool IsNodeMaxedOut(IReadOnlyCollection<ushort> unlockedUpgrades) {
        return GetCurrentLevel(unlockedUpgrades) >= MaxLevel;
    }
    /// <summary>
    /// Calculates the current level of a node based on the set of unlocked upgrades.
    /// </summary>
    /// <param name="node">The design-time node to check.</param>
    /// <param name="unlockedUpgrades">The player's set of unlocked upgrade IDs.</param>
    public int GetCurrentLevel(IReadOnlyCollection<ushort> unlockedUpgrades) {
        if (stages == null || stages.Count == 0 || unlockedUpgrades == null) return 0;

        int level = 0;
        foreach (var stage in stages) {
            if (stage.upgrade != null && unlockedUpgrades.Contains(stage.upgrade.ID)) {
                level++;
            } else {
                // Since levels are sequential, we stop at the first un-purchased one.
                break;
            }
        }
        return level;
    }
    /// <summary>
    /// Gets the available stage for a specific node, if any.
    /// </summary>
    /// <param name="tree"> Needed to calculate the resource cost
    /// <returns>The UpgradeStage to be purchased next, or null if the node is maxed out.</returns>
    public UpgradeRecipeSO GetUpgradeData(IReadOnlyCollection<ushort> unlockedUpgrades, UpgradeTreeDataSO tree) {
        
        if (stages == null || unlockedUpgrades == null || stages.Count == 0) return null;
        var isMaxed = IsNodeMaxedOut(unlockedUpgrades);
        int currentLevel = GetCurrentLevel(unlockedUpgrades);
        // Determine which stage's info to show (the next one, or the last one if maxed)
        int infoStageIndex = isMaxed ? MaxLevel - 1 : currentLevel;
        if (!isMaxed && ArePrerequisitesMet(unlockedUpgrades)) {
            UpgradeStage nextStageToUnlock = stages[currentLevel];
            return tree.GetPreparedRecipeForStage(nextStageToUnlock);
        } else {
            return stages[infoStageIndex].upgrade;
        }
    }

    public bool ArePrerequisitesMet(IReadOnlyCollection<ushort> unlockedUpgrades) {
        if (prerequisiteNodesAny == null || prerequisiteNodesAny.Count == 0) {
            return true; // root nodes available by default
        }
        return UnlockedAtFirstPrereqStage ? 
            prerequisiteNodesAny.Any(p => p != null && p.GetCurrentLevel(unlockedUpgrades) > 0) 
        :  prerequisiteNodesAny.Any(p => p != null && p.IsNodeMaxedOut(unlockedUpgrades));
    }

    internal UpgradeNodeState GetState(IReadOnlyCollection<ushort> unlockedUpgrades,bool canAfford) {
        int currentLevel = GetCurrentLevel(unlockedUpgrades);
        bool isMaxed = currentLevel >= MaxLevel;
        bool prereqsMet = ArePrerequisitesMet(unlockedUpgrades);
        if (!prereqsMet && currentLevel == 0) {
            return UpgradeNodeState.Locked;
        } else if (isMaxed) {
            return UpgradeNodeState.Purchased;
        } else if (canAfford) {
            return UpgradeNodeState.Purchasable;
        } else { // prereqsMet and currentLevel is 0
            return UpgradeNodeState.Unlocked;
        }
    }
}