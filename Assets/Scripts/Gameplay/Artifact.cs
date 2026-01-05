using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// This should be an entity but just have it a monobehaviour I don't care anymore it wont be multiplayer
public class Artifact : MonoBehaviour {
    public List<TileBase> tiles;
    private BiomeDataSO biomeData;
    private RectInt _footprintRect;
    private int _breakCount;
    private int _breakReq;

    internal void Init(StructurePlacementResult data) {
        transform.position = new(data.bottomLeftAnchor.x, data.bottomLeftAnchor.y,0);
        _footprintRect = new RectInt(data.bottomLeftAnchor, Vector2Int.one * 3);
        _breakCount = 0;
        _breakReq = _footprintRect.width * _footprintRect.height; // TODO this will not work if some areas are air
        biomeData = App.ResourceSystem.GetBiomeData((ushort)data.biome);
        ChunkManager.OnTileChanged += TileChanged;
    }
    private void OnDisable() {
        ChunkManager.OnTileChanged -= TileChanged;
    }

    private void TileChanged(Vector3Int tilePos, ushort tile) {
        if(tile == ResourceSystem.AirID) {
            Vector2Int pos = new(tilePos.x,tilePos.y);
            if (_footprintRect.Contains(pos)) {
                _breakCount++;
            }
            if(_breakCount >= _breakReq) {
                // All blocks broken!
                AddBiomeEffects(biomeData);
            }
        }
    }

    // Different ways to implement the adding here:
    // For buffs:
    // 1. You notify BiomeBuffSpawner that the buff/ability should stay, maybe the buff/ability itself needs to know if its a biome or normal buff so the strength can vary
    // 2. You remove the biome buff/ability and add a unique permanent buff/ability that never runs out
    
    // I guess both seem like locigal implementations, second option seems more generic. For example if we'd want the 
    // transparency to turn into an active ability we could do that easily because its a different ability
    private void AddBiomeEffects(BiomeDataSO data) {
        if (data == null) {
            Debug.LogError("biome data not defined");
        }
        var buffs = data.BiomeTempBuffs;
        var abilities = data.BiomeTempAbilities;
        if (buffs != null || buffs.Count > 0) {
            // Gain buffs
            foreach (var buff in buffs) {
                // Maybe we don't gain the buff again, we just don't remove it when we exit?
                NetworkedPlayer.LocalInstance.PlayerStats.TriggerBuff(buff);
            }
        }
        if (abilities != null || abilities.Count > 0) {
            foreach (var ability in abilities) {
                NetworkedPlayer.LocalInstance.PlayerAbilities.AddAbility(ability);
            }
        }
    }
}
