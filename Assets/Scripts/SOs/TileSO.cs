using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "TileSO", menuName = "ScriptableObjects/TileSO")]
public class TileSO : RuleTile, IIdentifiable {
    [Header("Game Properties")]
    public short maxDurability = 10; // How many "hits" it takes to break. -1 means non solid.
    public int biomeIndex = -1; // Used in the shader to know what the texture should be, set as -1 only if not used by shader
    ushort IIdentifiable.ID => ID;
    public ushort ID; 
    public bool IsSolid => maxDurability != -1;
    private const float INDEX_SCALE = 16.0f;

    public DropTableSO dropTable;   // Assign the ScriptableObject defining drops
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
    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData) {
        base.GetTileData(position, tilemap, ref tileData);
        if (biomeIndex == -1) return;
        float encodedIndex = (float)biomeIndex / INDEX_SCALE;
        tileData.color = new Color(encodedIndex, 0f, 0f, 1f).gamma;
    }
}
