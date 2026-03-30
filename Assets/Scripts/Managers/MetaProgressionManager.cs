// MetaProgressionManager.cs
using r8teful;
using Sirenix.OdinInspector.Editor.Drawers;
using System;
using System.Collections.Generic;
using UnityEditor.Overlays;
using UnityEditor.U2D.Tooling.Analyzer;
using UnityEngine;

public class MetaProgressionManager : PersistentSingleton<MetaProgressionManager>, ISaveable {

    // Runtime State
    private Dictionary<MetaUnlockStat, float> currentStats = new Dictionary<MetaUnlockStat, float>();
    private HashSet<ushort> unlockedIDs = new HashSet<ushort>();

    // Events for UI and Gameplay to listen to
    public event Action<MetaUnlockStat, float> OnStatUpdated;
    public event Action<MetaUnlockSO> OnNewUnlock;


    public bool IsUnlocked(ushort unlockID) => unlockedIDs.Contains(unlockID);

    public void AddStat(MetaUnlockStat type, float amount) {
        if (!currentStats.ContainsKey(type))
            currentStats[type] = 0;

        currentStats[type] += amount;

        // Notify UI that a stat changed
        OnStatUpdated?.Invoke(type, currentStats[type]);

        // Check if this new stat unlocks anything
        CheckUnlocks(type);
    }

    

    /// <summary>
    /// Returns current progress and target for UI bars
    /// </summary>
    public (float current, float target) GetProgress(MetaUnlockSO unlockable) {
        if (IsUnlocked(unlockable.ID)) return (unlockable.targetValue, unlockable.targetValue);

        float currentVal = currentStats.ContainsKey(unlockable.requiredStat) ? currentStats[unlockable.requiredStat] : 0f;
        return (currentVal, unlockable.targetValue);
    }


    private void CheckUnlocks(MetaUnlockStat typeChanged) {
        float currentVal = currentStats[typeChanged];

        foreach (var unlockable in App.ResourceSystem.GetAllUnlocks()) {
            // Skip if already unlocked or if it requires a different stat
            if (IsUnlocked(unlockable.ID) || unlockable.requiredStat != typeChanged)
                continue;

            if (currentVal >= unlockable.targetValue) {
                unlockedIDs.Add(unlockable.ID);
                OnNewUnlock?.Invoke(unlockable);
                Debug.Log($"Unlocked: {unlockable.displayID}!");
            }
        }
    }


    public void OnSave(r8teful.SaveData data) {
        // Convert Dictionary/HashSet to Lists for JSON serialization
        //foreach (var kvp in currentStats) {
        //    saveData.statKeys.Add(kvp.Key);
        //    saveData.statValues.Add(kvp.Value);
        //}
        //saveData.unlockedIDs.AddRange(unlockedIDs);

    }

    public void OnLoad(r8teful.SaveData data) {
        //currentStats.Clear();
        //for (int i = 0; i < saveData.statKeys.Count; i++) {
        //    currentStats[saveData.statKeys[i]] = saveData.statValues[i];
        //}
        // unlockedIDs = new HashSet<ushort>(saveData.unlockedIDs);
    }
}

// Helper class for saving Dictionary/HashSet via Unity's built-in JSON
[Serializable]
public class ProgressionSaveData {
    public List<StatType> statKeys = new List<StatType>();
    public List<float> statValues = new List<float>();
    public List<string> unlockedIDs = new List<string>();
}
public enum MetaUnlockStat {
    TilesDestroyed = 0,
    PlantsDestroyed = 1,
    DistanceTraveled = 10,
    WinAmount = 20,
    TotalResourcesSpent = 30
}