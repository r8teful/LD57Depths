using System.Collections.Generic;
using UnityEngine;
// An ability can be passive (like a permanent buff, or tool, or passive destruction)
// Or active (like a tool ability)

[CreateAssetMenu(fileName = "AbilityBaseSO", menuName = "ScriptableObjects/Abilities/AbilityBaseSO", order = 8)]
public class AbilitySO : ScriptableObject, IIdentifiable {
    [SerializeField] private ushort id;
    public ushort ID => id;
    public Sprite icon;
    public string displayName;
    [TextArea(4, 10)]
    public string description;
    public AbilityType type;
    public float cooldown; // Same as duration, this number is MULTIPLYING our base cooldown stat
    public List<StatDefault> StatTypes; // Initial stats
    public List<StatModifier> UpgradeValues; // What stats can be upgraded and by how much 

    public List<ScriptableObject> effects;
    public List<CosmeticData> costumes;

}

public enum AbilityType { Passive, Active }