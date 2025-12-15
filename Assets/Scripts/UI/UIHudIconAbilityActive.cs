using System;
using System.Collections;
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
        Debug.Log($"Active Time changed: {time}");
        timeText.text = FormatSeconds(time);
        timeText.color = Color.green;
        
    }
    private void OnAbilityCooldownChanged(float time) {
        _progressImage.fillAmount = time;
        //timeText.text = time.ToString();
        //timeText.color = Color.white;
    }
    string FormatSeconds(float sec) {
        if (sec <= 0) return "0s";
        int s = Mathf.CeilToInt(sec);
        if (s >= 60) return $"{s / 60}m {s % 60}s";
        return $"{s}s";
    }
}