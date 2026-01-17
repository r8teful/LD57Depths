using System;

public static class XPEvents {
    public static event Action OnUILevelReady; // Ui calls this

    public static event Action<int> OnGainXP;

    // The PlayerExperienceManager triggers this event
    public static event Action<int> OnLevelUpReady;

    // Ratio, XP amount. 
    public static event Action<float, int> OnXPChanged;

    public static void TriggerGainXP(int amount) => OnGainXP?.Invoke(amount);
    public static void TriggerLevelUp(int newLevel) => OnLevelUpReady?.Invoke(newLevel);
    public static void TriggerXPGained(float ratio, int amount) => OnXPChanged?.Invoke(ratio,amount);
    public static void TriggerUIReady() => OnUILevelReady?.Invoke();
}