using System.Collections;
using UnityEngine;


[CreateAssetMenu(fileName = "EffectFish", menuName = "ScriptableObjects/AbilityEffects/Fish")]
public class FishGun : ScriptableObject, IEffectPassive {
    [SerializeField] GameObject Fish_Projectile;
    private GameObject _instantiatedGun;

    public void Apply(AbilityInstance instance, NetworkedPlayer player) {
        _instantiatedGun = Instantiate(Fish_Projectile, player.PlayerAbilities.AbilitySlot);
        _instantiatedGun.GetComponent<MiningFish>().Init(instance, player);
    }

    public void Remove(AbilityInstance instance, NetworkedPlayer player) {
        Destroy(_instantiatedGun);
    }
}