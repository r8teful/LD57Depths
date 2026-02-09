
using System;
using UnityEngine;

public static class UpgradeCalculator {
    public static float CalculateCostForLevel(int level, float baseValue, float linearIncrease, float expIncrease) {
        return baseValue + Mathf.Pow(expIncrease,level) + (linearIncrease*level);
    }
    public static float[] CalculatePointArray(int levels, float baseValue, float linearIncrease, float expIncrease) {
        float[] points = new float[levels];
        for (int i = 0; i < levels; i++) {
            points[i] = CalculateCostForLevel(i, baseValue, linearIncrease, expIncrease);
        }
        return points;
    }
    public static float CalculateUpgradeChange(float current, StatModifyType type, float increaseAmount) {
        switch (type) {
            case StatModifyType.Add:
                return current + increaseAmount;
            case StatModifyType.PercentAdd:
                return current * (1 + increaseAmount);
            case StatModifyType.PercentMult:
                return current * increaseAmount;
            default:
                Debug.LogWarning("Fallback, coudn't find approriate increase type calculation!");
                return current;
        }
    }

    public static float CalculateUpgradeChange(float current, ValueModifier modifier) {
        return CalculateUpgradeChange(current, modifier.Type, modifier.Value);
    }
    public static float CalculateUpgradeChange(float current, StatModifier modifier) {
        return CalculateUpgradeChange(current, modifier.Type, modifier.Value);
    }
}