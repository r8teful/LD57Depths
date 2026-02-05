using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Shows active abilities with cooldown and active length
public class UIHudIconAbilityActive : UIHudIconBase {
    [SerializeField] private Image _progressImage;
    [SerializeField] private TextMeshProUGUI timeText; // We won't really have a time text but just for now
    internal void Init(AbilityInstance ability) {
        InitBase(ability.Data.icon, ability.Data.displayName, "");
        // Subscribe to ability things
        ability.OnCooldownChanged += OnAbilityCooldownChanged;
        ability.OnActiveTimeChanged+= OnAbilityActiveTimeChanged;
        ability.OnReady += OnAbilityReady;
        ability.OnUsed += OnAbilityUsed;
    }


    private void OnAbilityReady() {
        //_iconImage.color = Color.green;
    }
    private void OnAbilityUsed() {
        //_iconImage.color = Color.white;
    }

    private void OnAbilityActiveTimeChanged(float time) {
        //Debug.Log($"Active Time changed: {time}");
        timeText.text = FormatSeconds(time);
        timeText.color = Color.green;
        
    }
    private void OnAbilityCooldownChanged(float time,float max) {
        float progress = time / Mathf.Max(0.001f,max);
        _progressImage.fillAmount = progress;
        timeText.text = FormatSeconds(time);
        timeText.color = Color.red;
    }
    string FormatSeconds(float sec) {
        if (sec <= 0) return "0s";
        int s = Mathf.CeilToInt(sec);
        if (s >= 60) return $"{s / 60}m {s % 60}s";
        return $"{s}s";
    }
}