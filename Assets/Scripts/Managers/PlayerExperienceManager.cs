using System;
using UnityEngine;

public class PlayerExperienceManager : MonoBehaviour, INetworkedPlayerModule {
    [Header("Player Stats")]
    private LevelData levelData;
    private bool isLevelUpSequenceActive;
    private NetworkedPlayer _player;

    public int InitializationOrder => 99;

    public void InitializeOnOwner(NetworkedPlayer playerParent) {
        levelData ??= new LevelData();
        _player = playerParent;
        // Ensure the required XP is calculated correctly on start
        levelData.CalculateNextLevelThreshold();
        XPEvents.OnGainXP += AddExperience;
        XPEvents.OnUILevelReady += CommitLevelUp;
    }

    private void AddExperience(int amount) {
        levelData.currentXP += amount;
        XPEvents.TriggerXPGained(levelData.ProgressNormalized, levelData.currentXP);
        Debug.Log($"Gained {amount} XP. Total: {levelData.currentXP}/{levelData.xpToNextLevel}");
        TryProcessLevelUp();
    }
    
    private void TryProcessLevelUp() {
        // Guard Clause: If UI is already busy handling a level up, stop here.
        if (isLevelUpSequenceActive) return;

        // Check if we actually have enough XP
        if (levelData.currentXP >= levelData.xpToNextLevel) {
            StartLevelUpSequence();
        }
    }

    private void StartLevelUpSequence() {
        isLevelUpSequenceActive = true;

        int levelToBecome = levelData.currentLevel + 1;
        
        // Tell reward manager to create rewards
        _player.PlayerReward.GenerateRewards(levelToBecome);

        XPEvents.TriggerLevelUp(levelToBecome);
        // UI will then call CommitLevelUp when we are ready to actually increment the level and start new level progress
    }

    /// <summary>
    /// Call this function when we've selected level rewards
    /// </summary>
    public void CommitLevelUp(IExecutable choice) {
        // Tell reward manager to execute the reward we have chosen
        _player.PlayerReward.ExecuteReward(choice);

        // Subtract the cost of the level
        levelData.currentXP -= levelData.xpToNextLevel;
        // Increase Level
        levelData.currentLevel++;
        // Recalculate requirement for the *new* level
        levelData.CalculateNextLevelThreshold();
        isLevelUpSequenceActive = false;

        XPEvents.TriggerXPGained(levelData.ProgressNormalized,levelData.currentXP);
        Debug.Log($"<color=green>LEVEL UP! Now Level {levelData.currentLevel}</color>");
        TryProcessLevelUp(); // Recursion check 
    }
    public void DEBUGAddXP() {
        //ExperienceEvents.TriggerGainXP(50);
    }

}