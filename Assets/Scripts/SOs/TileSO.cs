using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "TileSO", menuName = "ScriptableObjects/TileSO")]
public class TileSO : RuleTile, IIdentifiable {
    [Header("Game Properties")]
    public short maxDurability = 10; // How many "hits" it takes to break. -1 means non solid.
    [HideInInspector]  // We now set it dynamically based on biome ( so Y color value in the shader )
    public int textureIndex = -1; // Used in the shader to know what the texture should be, set as -1 only if not used by shader
    ushort IIdentifiable.ID => ID;
    public ushort ID; 
    public bool IsSolid => maxDurability != -1;
    private const float INDEX_SCALE = 16.0f;

    public ItemData drop;
    public GameObject breakEffectPrefab; // Optional: particle effect on break
    public GameObject hitEffectPrefab; // Optional: particle effect on hit
    public List<TileBase> breakVersions;
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
    public TileBase GetCrackTileForDurability(float currentDurability) {
        if (breakVersions == null || breakVersions.Count == 0) return null;
        if (currentDurability < 0) return null;
        float r = GetDurabilityRatio(currentDurability);
        // Map ratio -> index:
        // r == 1  -> index 0 (no crack)
        // r == 0  -> index versions.Length - 1 (fully cracked)
        int len = breakVersions.Count;
        int index = Mathf.FloorToInt((1f - r) * len);
        index = Mathf.Clamp(index, 0, len - 1);

        return breakVersions[index];
    }

    // Shader uses BiomeBackgrounds to index with textureIndex to set the right textue!
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData) {
        base.GetTileData(position, tilemap, ref tileData);
        if (textureIndex == -1 || textureIndex == ResourceSystem.InvalidID) return;
        // Is it possible to set this tectureIndex at runtime? 
        float encodedIndex = (float)textureIndex / INDEX_SCALE;
        tileData.color = new Color(encodedIndex, 0f, 0f, 1f).gamma;
    }
}
