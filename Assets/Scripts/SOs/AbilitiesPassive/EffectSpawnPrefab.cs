using UnityEngine;

[CreateAssetMenu(fileName = "EffectSpawnPrefab", menuName = "ScriptableObjects/AbilityEffects/EffectSpawnPrefab")]
public class EffectSpawnPrefab : ScriptableObject, IEffectPassive {
    [SerializeField] GameObject prefabToSpawn;
    private GameObject _instantiatedEffect;

    public void Apply(AbilityInstance instance, NetworkedPlayer player) {
        _instantiatedEffect = Instantiate(prefabToSpawn, player.PlayerAbilities.AbilitySlot);
        if (_instantiatedEffect.TryGetComponent<IInitializableAbility>(out var initComp)) {
            initComp.Init(instance, player);
            instance.SetGameObject(_instantiatedEffect);
        } else {
            Debug.LogError($"Prefab '{prefabToSpawn.name}' has no component implementing IInitializableAbility.");
            Destroy(_instantiatedEffect);
            _instantiatedEffect = null;
        }
    }

    public void Remove(AbilityInstance instance, NetworkedPlayer player) {
        if (_instantiatedEffect != null) {
            Destroy(_instantiatedEffect);
            _instantiatedEffect = null;
        }
    }
}