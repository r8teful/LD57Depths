using System;

public static class RewardEvents {
    public static event Action OnUILevelReady; // Ui calls this with the reward we have chosen (null if skipped)

    public static event Action<int> OnGainXP;

    // The PlayerExperienceManager triggers this event
    public static event Action<int> OnLevelUpReady;

    // Ratio, XP amount. 
    public static event Action<float, int> OnXPChanged;

    public static event Action OnChestOpen; // UI reacts to this

    public static void TriggerGainXP(int amount) => OnGainXP?.Invoke(amount);
    public static void TriggerLevelUp(int newLevel) => OnLevelUpReady?.Invoke(newLevel);
    public static void TriggerXPGainedUI(float ratio, int amount) => OnXPChanged?.Invoke(ratio,amount);
    public static void TriggerUIReady() => OnUILevelReady?.Invoke();

    public static void TriggerOpenChest() => OnChestOpen?.Invoke();
}