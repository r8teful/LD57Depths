using System.Collections;
using UnityEngine;


[CreateAssetMenu(fileName = "EffectFish", menuName = "ScriptableObjects/AbilityEffects/Fish")]
public class FishGun : ScriptableObject, IEffectPassive {
    [SerializeField] GameObject Fish_Projectile;

    public void Apply(AbilityInstance instance, NetworkedPlayer player) {
        var g = Instantiate(Fish_Projectile, player.PlayerAbilities.AbilitySlot);
        g.GetComponent<MiningFish>().Init(instance, player);
    }

    public void Remove(AbilityInstance instance, NetworkedPlayer player) {
    
    }
}