
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

}