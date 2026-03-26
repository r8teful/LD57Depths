using Sirenix.OdinInspector;
using UnityEngine;

// Hold gameplay things of the biome
// For example, what buffs the player gets when entering the biome, and informational things like what name it has, etc
[CreateAssetMenu(fileName = "BiomeDataSO", menuName = "ScriptableObjects/WorldGen/BiomDataSO", order = 1)]
public class BiomeDataSO : ScriptableObject, IIdentifiable {
    public BiomeType BiomeType;

    // These where all lists but an ability or buff can have several effects. Giving us simpler logic
    [SerializeField] private bool hasBuffs;
    // Temporary ability and buff gained when entering biome
    [ShowIf("hasBuffs")]
    public AbilitySO BiomeTempAbility; 
    [ShowIf("hasBuffs")]
    public BuffSO BiomeTempBuff; 

    // Permanent ability and buff gained when finding artifact
    [ShowIf("hasBuffs")]
    public AbilitySO BiomePermanentAbility; 
    [ShowIf("hasBuffs")]
    public BuffSO BiomePermanentBuff;

    public Color BiomeTextColour;
    public Color BiomeHighlightColour;
    public ushort ID => (ushort)BiomeType;
}