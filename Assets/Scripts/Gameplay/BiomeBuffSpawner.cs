using System;
using System.Collections;
using UnityEngine;
public class BiomeBuffSpawner : MonoBehaviour {
    private NetworkedPlayer _player;
    private AbilityInstance _instance;
    private BuffHandle _currentBiomeBuff;

    internal void Init(AbilityInstance instance, NetworkedPlayer player) {
        _player = player;
        _instance = instance;
        BiomeManager.Instance.OnNewClientBiome += NewClientBiome;
    }

    private void Awake() {
    }
    private void OnDestroy() {
        BiomeManager.Instance.OnNewClientBiome -= NewClientBiome;
    }

    private void NewClientBiome(BiomeType oldB, BiomeType newB) {
        Debug.Log($"Buff new biome! {newB}");
        _currentBiomeBuff?.Remove();
        if(newB == BiomeType.None || newB == BiomeType.Trench || newB == BiomeType.Surface) {
            return; // Don't have buffs
        }
        var b = App.ResourceSystem.GetBuffByID((ushort)((ushort)newB + 200)); // So ugly omg
        if (b == null) return;
        _currentBiomeBuff = _player.PlayerStats.TriggerBuff(b);
        // Okay this works, but now what if a biome needs to spawn an an ability? wed say
        //_player.PlayerAbilities.AddAbility() and then .RemoveAbility that's easy enough!
    }
}