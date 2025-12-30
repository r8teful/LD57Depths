using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class BiomeBuffSpawner : MonoBehaviour {
    private NetworkedPlayer _player;
    private AbilityInstance _instance;

    private readonly List<AbilitySO> _currentAbilities = new();        
    private readonly List<BuffHandle> _currentBiomeBuffs = new();


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

        // Remove previously-applied biome effects (abilities + buffs)
        RemoveCurrentBiomeEffects();

        if (newB == BiomeType.None || newB == BiomeType.Trench || newB == BiomeType.Surface) {
            return;
        }

        var b = App.ResourceSystem.GetBiomeData((ushort)newB);
        if (b == null) return;

        foreach (var ability in b.BiomeTempAbilities) {
            if (ability == null) continue;

            // Avoid double-adding the same ability
            if (_currentAbilities.Contains(ability)) continue;

            _currentAbilities.Add(ability);
            _player.PlayerAbilities.AddAbility(ability);
        }

        // I guess we need to have these buffs under some unique "biome" icon or something, will look into this later 
        foreach (var buff in b.BiomeTempBuffs) {
            if (buff == null) continue;

            var inst = _player.PlayerStats.TriggerBuff(buff);
            if (inst != null) {
                _currentBiomeBuffs.Add(inst);
            }
        }
    }

    private void RemoveCurrentBiomeEffects() {
        // Remove abilities that were given by the biome
        if (_currentAbilities.Count > 0) {
            foreach (var ability in _currentAbilities) {
                if (ability == null) continue;
                _player.PlayerAbilities.RemoveAbility(ability);
            }
            _currentAbilities.Clear();
        }

        // Remove active buff instances
        if (_currentBiomeBuffs.Count > 0) {
            foreach (var buffInstance in _currentBiomeBuffs) {
                buffInstance?.Remove();
            }
            _currentBiomeBuffs.Clear();
        }
    }
}