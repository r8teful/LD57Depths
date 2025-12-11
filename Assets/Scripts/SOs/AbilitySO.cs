using System;
using System.Collections.Generic;
using UnityEngine;
// An ability can be passive (like a permanent buff, or tool, or passive destruction)
// Or active (like a tool ability)

[CreateAssetMenu(fileName = "AbilityBaseSO", menuName = "ScriptableObjects/AbilityBaseSO", order = 8)]
public class AbilitySO : ScriptableObject, IIdentifiable {
    [SerializeField] private ushort id;
    public ushort ID => id;
    public Sprite icon;
    public string displayName;
    public AbilityType type;
    public float cooldown;
    // Designer adds SOs that implement IAbilityEffect or IPassiveEffect
    public List<ScriptableObject> effects;

    internal float GetBaseModifierForStat(StatType stat) {
        return 1; // TODO
    }
}
public enum AbilityType { Passive, Active }