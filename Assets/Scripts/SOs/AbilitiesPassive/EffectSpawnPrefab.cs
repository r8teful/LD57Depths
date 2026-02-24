using UnityEngine;

[CreateAssetMenu(fileName = "EffectSpawnPrefab", menuName = "ScriptableObjects/AbilityEffects/EffectSpawnPrefab")]
public class EffectSpawnPrefab : ScriptableObject, IEffectPassive, IExecutable {
    [SerializeField] GameObject prefabToSpawn;
    private GameObject _instantiatedEffect;

    public void Apply(AbilityInstance instance, PlayerManager player) {
        if (prefabToSpawn == null) {
            Debug.LogWarning("Couldn't spawn effect prefab because ists not been assigned");
            return;
        }
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

    public void Execute(ExecutionContext context) {

    }

    public UIExecuteStatus GetExecuteStatus() {
        return null;
    }

    public void Remove(AbilityInstance instance, PlayerManager player) {
        if (_instantiatedEffect != null) {
            Destroy(_instantiatedEffect);
            _instantiatedEffect = null;
        }
    }
}