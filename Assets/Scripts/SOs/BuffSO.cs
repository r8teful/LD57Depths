using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "BuffSO", menuName = "ScriptableObjects/Upgrades/BuffSO", order = 7)]
// Ability that changes the stat
public class BuffSO : ScriptableObject, IIdentifiable {
    [SerializeField] private ushort id;
    public ushort ID => id;
    public List<StatModifier> Modifiers;
    public string Title;
    public string Description;
    public Sprite Icon;

    public float GetDuration() {
        var dur = Modifiers.FirstOrDefault(m => m.Stat == StatType.Duration);
        return dur == null ? -1 : dur.Value; // -1 meaning the duration goes on indefinately
    }
}