using UnityEngine;

public class PlayerExperienceManager : MonoBehaviour, IPlayerModule {
    [Header("Player Stats")]
    private LevelData levelData;
    private PlayerManager _player;

    public int InitializationOrder => 99;

    public void InitializeOnOwner(PlayerManager playerParent) {
        if (!enabled) return;
        levelData ??= new LevelData();
        _player = playerParent;
        // Ensure the required XP is calculated correctly on start
        levelData.CalculateNextLevelThreshold();
        RewardEvents.OnGainXP += AddExperience;
        RewardEvents.OnUILevelReady += CommitLevelUp;
    }

    private void AddExperience(int amount) {
        levelData.currentXP += amount;
        RewardEvents.TriggerXPGainedUI(levelData.ProgressNormalized, levelData.currentXP);
        //Debug.Log($"Gained {amount} XP. Total: {levelData.currentXP}/{levelData.xpToNextLevel}");
        TryProcessLevelUp();
    }
    
    private void TryProcessLevelUp() {
        // Check if we actually have enough XP
        if (levelData.currentXP >= levelData.xpToNextLevel) {
            QueueLevelup();
        }
    }

    private void QueueLevelup() {

        // We calculate the level number NOW, so the UI shows the correct number 
        int levelToBecome = levelData.currentLevel + 1;
        
        // Add to the centralized queue
        GameSequenceManager.Instance.AddEvent(shouldPause: true,
            onStart: () => {
                _player.PlayerReward.GenerateRewardsLevel(levelToBecome);
                RewardEvents.TriggerLevelUp(levelToBecome);
            },
            onFinish: () => {
                // This logic is handled by CommitLevelUp below which is called from UI
            }
        );     
    }

    /// <summary>
    /// Call this function when we've selected level rewards
    /// </summary>
    public void CommitLevelUp() {
        // Subtract the cost of the level
        levelData.currentXP -= levelData.xpToNextLevel;
        // Increase Level
        levelData.currentLevel++;
        // Recalculate requirement for the *new* level
        levelData.CalculateNextLevelThreshold();

        RewardEvents.TriggerXPGainedUI(levelData.ProgressNormalized,levelData.currentXP);
        Debug.Log($"<color=green>LEVEL UP! Now Level {levelData.currentLevel}</color>");
        TryProcessLevelUp(); // Recursion check 

        GameSequenceManager.Instance.AdvanceSequence(); // connected to level up sequence.
    }
    public void DEBUGAddXP() {
        //ExperienceEvents.TriggerGainXP(50);
    }

}