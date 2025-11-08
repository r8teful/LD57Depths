using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeTierSO", menuName = "ScriptableObjects/Upgrades/UpgradeTierSO")]
// The resources in an upgrade recipe are within a set of tiers, this is the definition of a tier.
// For example, a level 5 speed upgrade could be a Tier 1 None, which could have Iron and Gold in it, and a Tier 0 Fungal, which
// would have Mushroom and Spore. Then the upgrade system will calculate an approriate recipe from those 4 items
public class UpgradeTierSO : ScriptableObject {
    public int Tier; // The higher the tier, the better resources a recipe will require. Tiers will go up periodicly each level
    
    public List<ItemData> ItemsInTier; // The actuall itempool of that tier
}