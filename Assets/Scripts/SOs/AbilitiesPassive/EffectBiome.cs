using System.Collections;
using UnityEngine;


[CreateAssetMenu(fileName = "EffectBiome", menuName = "ScriptableObjects/AbilityEffects/EffectBiome")]
public class EffectBiome : ScriptableObject, IEffectPassive {
    [SerializeField] GameObject prefab;
    private GameObject _instantiatedObject;
    public void Apply(AbilityInstance instance, NetworkedPlayer player) {
        _instantiatedObject = Instantiate(prefab, player.PlayerAbilities.AbilitySlot);
        _instantiatedObject.GetComponent<BiomeBuffSpawner>().Init(instance, player);
    }

    public void Remove(AbilityInstance instance, NetworkedPlayer player) {
        Destroy(_instantiatedObject);
    }
}