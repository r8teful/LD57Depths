﻿
using UnityEngine;

public static class UpgradeCalculator {
    public static float CalculateTotalPoints(int level, float baseValue, float linearIncrease, float expIncrease) {
        return baseValue + Mathf.Pow(expIncrease,level) + (linearIncrease*level);
    }
    public static float[] CalculatePointArray(int levels, float baseValue, float linearIncrease, float expIncrease) {
        float[] points = new float[levels];
        for (int i = 0; i < levels; i++) {
            points[i] = CalculateTotalPoints(i, baseValue, linearIncrease, expIncrease);
        }
        return points;
    }
    public static float CalculateUpgradeIncrease(float current, IncreaseType type, float increaseAmount) {
        if(type== IncreaseType.Add) {
            return current + increaseAmount;
        } else if (type == IncreaseType.Multiply) {
            return current * increaseAmount;
        }
        //fallback
        return current;
    }
    public static float CalculateUpgradeIncrease(float current, UpgradeRecipeValue data) {
        return CalculateUpgradeIncrease(current,data.increaseType,data.value);
    }
}