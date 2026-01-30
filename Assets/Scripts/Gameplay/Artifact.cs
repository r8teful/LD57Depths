using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

// This should be an entity but just have it a monobehaviour I don't care anymore it wont be multiplayer
public class Artifact : MonoBehaviour {
    private BiomeDataSO biomeData;
    private RectInt _footprintRect;
    private int _breakCount;
    private int _breakReq;
    private bool _revealed;
    internal void Init(StructurePlacementResult data, BiomeType biome) {
        transform.position = new(data.bottomLeftAnchor.x, data.bottomLeftAnchor.y,0);
        _footprintRect = new RectInt(data.bottomLeftAnchor, Vector2Int.one * 3);
        _breakCount = 0;
        _breakReq = _footprintRect.width * _footprintRect.height; // TODO this will not work if some areas are air
        biomeData = App.ResourceSystem.GetBiomeData((ushort)biome);
        ChunkManager.OnTileChanged += TileChanged;
    }
    private void OnDisable() {
        ChunkManager.OnTileChanged -= TileChanged;
    }

    private void TileChanged(Vector3Int tilePos, ushort tile) {
        if (_revealed) return;
        if(tile == ResourceSystem.AirID) {
            Vector2Int pos = new(tilePos.x,tilePos.y);
            if (_footprintRect.Contains(pos)) {
                _breakCount++;
            }
            if(_breakCount >= _breakReq) {
                // All blocks broken!
                _revealed = true;
                RemoveTempBiomeEffects();
                AddBiomeEffects(biomeData);
            }
        }
    }

    private void RemoveTempBiomeEffects() {
        // Tell BiomeBuffSpawner to remove temp effects for biome
        var biomeBuffAbility = PlayerManager.LocalInstance.PlayerAbilities.GetAbilityInstance(ResourceSystem.BiomeBuffID);
        if(biomeBuffAbility.Object.TryGetComponent<BiomeBuffSpawner>(out var buffSpawner)){
            buffSpawner.RemoveCurrentBiomeEffects(); // Seems messy, its either this or an event
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
        var ability = data.BiomePermanentAbility;
        var buff = data.BiomePermanentBuff;
        if (ability != null) {
            PlayerManager.LocalInstance.PlayerAbilities.AddAbility(ability);
        }
        if (buff != null) {
            PlayerManager.LocalInstance.PlayerStats.TriggerBuff(buff);
        }
    }
}
