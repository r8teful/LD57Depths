using UnityEngine;
public class BiomeBuffSpawner : MonoBehaviour, IInitializableAbility {
    private PlayerManager _player;
    private AbilityInstance _instance;

    private AbilitySO _currentAbility;        
    private BuffHandle _currentBiomeBuff;

    public void Init(AbilityInstance instance, PlayerManager player) {
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

        if (b.BiomeTempAbility != null) {
            _currentAbility = b.BiomeTempAbility;
            _player.PlayerAbilities.AddAbility(b.BiomeTempAbility);
        }
        if (b.BiomeTempBuff != null) { 
            var inst = _player.PlayerStats.TriggerBuff(b.BiomeTempBuff);
            if (inst != null) {
                _currentBiomeBuff = inst;
            }
        }
        
    }

    public void RemoveCurrentBiomeEffects() {
        // Remove ability 
        if(_currentAbility != null) {
            _player.PlayerAbilities.RemoveAbility(_currentAbility);
            _currentAbility = null;
        }
        // Remove active buff 
        if (_currentBiomeBuff != null) {
            _currentBiomeBuff?.Remove();
            _currentBiomeBuff = null;
        }
    }
}