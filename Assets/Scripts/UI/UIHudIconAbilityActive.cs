using System;
using System.Collections;
using TMPro;
using UnityEngine;

// Shows active abilities with cooldown and active length
public class UIHudIconAbilityActive : UIHudIconBase {

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
        _iconImage.color = Color.green;
    }
    private void OnAbilityUsed() {
        _iconImage.color = Color.white;
    }

    private void OnAbilityActiveTimeChanged(float time) {
        Debug.Log($"Active Time changed: {time}");
        timeText.text = FormatSeconds(time);
        timeText.color = Color.green;
        
    }
    private void OnAbilityCooldownChanged(float time) {
        // THINK ABOUT IT!!
        // ability cooldown is just the cooldown until we can activate the ability next, we should show this only when the ability isn't currently active
        // If it is active, we have to show how long it will be active for

        // the wierd thing is that the buff itself is not on our AbilityInstance, it is on the lazer abilityinstance. So we have two options:
        // We either get a reference to the BuffInstance that IS on the lazer instance, or, we just say "brimstone has been fired with this buffInstance".
        // 
        timeText.text = time.ToString();
        timeText.color = Color.white;
    }
    string FormatSeconds(float sec) {
        if (sec <= 0) return "0s";
        int s = Mathf.CeilToInt(sec);
        if (s >= 60) return $"{s / 60}m {s % 60}s";
        return $"{s}s";
    }
}