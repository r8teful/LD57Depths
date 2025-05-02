using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic; 

[System.Serializable]
public class ItemDropInfo {
    public GameObject itemPrefab; // Prefab of the item to drop (must have NetworkObject)
    public int minAmount = 1;
    public int maxAmount = 1;
    [Range(0f, 1f)] public float dropChance = 1.0f; // Chance this specific item drops (0 to 1)
}

// ScriptableObject to define a collection of possible drops
[CreateAssetMenu(fileName = "DropTable", menuName = "ScriptableObjects/Drop Table")]
public class DropTable : ScriptableObject {
    public List<ItemDropInfo> drops;
}

[CreateAssetMenu(fileName = "TileSO", menuName = "ScriptableObjects/TileSO")]
public class TileSO : RuleTile 
{
    [Header("Game Properties")]
    public int maxDurability = 10; // How many "hits" it takes to break. 0 or less means indestructible.
    public int tileID; 
    public DropTable dropTable;   // Assign the ScriptableObject defining drops
    public GameObject breakEffectPrefab; // Optional: particle effect on break
    public GameObject hitEffectPrefab; // Optional: particle effect on hit
    public List<TileBase> breakVersions;
    public BiomeType associatedBiome = BiomeType.None;
    // We might add other properties here later:
    // public ToolType requiredTool;
    // public int minToolLevel;
    // public AudioClip breakSound;
    // public AudioClip hitSound;
    public float GetDurabilityRatio(float current) {
        if(maxDurability<=0)
            return -1; // error
        return Mathf.Clamp(current / maxDurability, 0f, 1f);
    }
}
public enum BiomeType {
    None, 
    Trench,
    Cave,
    Coral,
    Ocean
}