using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// An ability can be passive (like a permanent buff, or tool, or passive destruction)
// Or active (like a tool ability)

[CreateAssetMenu(fileName = "AbilityBaseSO", menuName = "ScriptableObjects/Abilities/AbilityBaseSO", order = 8)]
public class AbilitySO : ScriptableObject, IIdentifiable {
    [SerializeField] private ushort id;
    public ushort ID => id;
    public Sprite icon;
    public string displayName;
    public AbilityType type;
    public float cooldown;
    public bool isTimed; // timed meaning that when we reach cooldown, a timed effect will happen (such as a temp buff)
    public List<StatTypesBase> statTypes; // Literally just used for diplaying them in the debug UI atm
    public List<ScriptableObject> effects;
    public List<CosmeticData> costumes;

    internal float GetBaseModifierForStat(StatType stat) {
        var b = statTypes.FirstOrDefault(s => s.stat == stat);
        return b != null ? b.baseMofifier : 1; 
    }
}
[System.Serializable]
public class StatTypesBase {
    public StatType stat;
    public StatModifyType modType; // If add, we show effective value, if mult, we show mult value
    public float baseMofifier;

    // This could have the "base" increase type for this stat right?
}
public enum AbilityType { Passive, Active }