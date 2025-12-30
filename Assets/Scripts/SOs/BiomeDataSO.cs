using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Hold gameplay things of the biome
// For example, what buffs the player gets when entering the biome, and informational things like what name it has, etc
[CreateAssetMenu(fileName = "BiomeDataSO", menuName = "ScriptableObjects/BiomDataSO", order = 1)]
public class BiomeDataSO : ScriptableObject, IIdentifiable {
    public BiomeType BiomeType;
    public List<AbilitySO> BiomeTempAbilities; // The abilities the biome gives when entered
    public List<BuffSO> BiomeTempBuffs; // The buffs the biome gives when entered                                    
    // Possibly two other lists of abilities and buffs that we get when we get the artifact!

    public ushort ID => (ushort)BiomeType;
}