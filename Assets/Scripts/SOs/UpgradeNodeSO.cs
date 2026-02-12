using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

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

    public float GetStageCost(int stageNum, UpgradeTreeDataSO tree) {
        if(stages.Count <= stageNum) return 0f;
        var stage = stages[stageNum];
        int currentStageLevel = stage.costTier;
        var c = tree.costsValues;
        float baseCost = UpgradeCalculator.CalculateCostForLevel(
            currentStageLevel, c.baseValue, c.linearIncrease, c.expIncrease);
        return baseCost * stage.costMultiplier;
    }
    public UpgradeTierSO GetStageTier(int tier) {
        if (stages.Count <= tier) return null;
        return stages[tier].upgradeItemPool;
    }
    public UpgradeStage GetStage(int stage) {
        if (stages.Count <= stage) return null;
        return stages[stage];
    }
    public UpgradeStage GetLastStage() {
        if (stages == null || stages.Count == 0) return null;
        return stages[^1]; // => stages.count - 1
    }
}
public enum UpgradeType {
    // MINING Lazer
    MiningLazerRange,
    MiningLazerDamage,
    MiningLazerHandling,
    MiningLazerSpecialNoFalloff,
    MiningLazerSpecialNoKnockback,
    MiningLazerSpecialCombo,
    MiningLazerSpecialBrimstone,
    // PLAYER SPEED
    PlayerSpeedMax,
    PlayerSpecialHandling,
    PlayerSpecialOreEmit,
    PlayerSpecialBlockOxygen,
    PlayerSpecialDiver,
    SpeedAcceleration,
    // OXYGEN
    OxygenMax,
    // UTILITY  
    PlayerLightRange,
    UtilityLightIntensity,
}
