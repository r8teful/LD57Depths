using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AbilityBaseSO", menuName = "ScriptableObjects/AbilityBaseSO", order = 7)]
// Ability that changes the stat
public class AbilityBaseSO : ScriptableObject, IIdentifiable {
    [SerializeField] private ushort id;
    public ushort ID => id;
    public List<StatModifier> Modifiers;
    public float Duration; // -1 is indefinate, but still not "permanent", such as a locational biome buff
                           // But would this not be better being separate, because when do we know that the buff would be finished? How would we get
                           // The reference to the SO and turn it off?? Something sexy we could do is abstracting how we "end" the buff, 
                           // If duration is more than 0, then it is the enumerator, but it could be anything really, as long as a certain condition is met.
    public string Title;
    public string Description;
    public Sprite Icon;
}