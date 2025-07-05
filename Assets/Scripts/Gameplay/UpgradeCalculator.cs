
using UnityEngine;

public static class UpgradeCalculator {
    public static float CalculateTotalPoints(int level, float baseValue, float linearIncrease, float expIncrease) {
        return baseValue + Mathf.Pow(expIncrease,level) + (linearIncrease*level);
    }

}