
using UnityEngine;

[System.Serializable]
public class LevelData {
    public int currentLevel;
    public int currentXP;
    public int xpToNextLevel;
    public float ProgressNormalized {
        get {
            if (xpToNextLevel <= 0f) return 0f;
            return Mathf.Clamp01(currentXP / (float)xpToNextLevel);
        }
    }

    public LevelData(int startLevel = 1, int startXP = 0) {
        currentLevel = startLevel;
        currentXP = startXP;
        CalculateNextLevelThreshold();
    }

    public void CalculateNextLevelThreshold() {
        // Just linear for now
        xpToNextLevel = currentLevel * 150;
    }
}