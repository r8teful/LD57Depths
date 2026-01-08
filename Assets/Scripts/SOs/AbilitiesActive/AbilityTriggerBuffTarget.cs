using System.Collections;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
[CreateAssetMenu(fileName = "EffectLazerBrimstone", menuName = "ScriptableObjects/AbilityEffects/Brimstone")]
public class AbilityTriggerBuffTarget : ScriptableObject, IEffectActive, IEffectBuff {
    [SerializeField] AbilitySO _targetAbility;
    [SerializeField] private BuffSO _buff;

    public BuffSO Buff => _buff;

    public void Execute(AbilityInstance source, NetworkedPlayer player) {
        // This ability should add multiplier buffs to the lazer passive
        Debug.Log("Triggering brimstone buff!");

        // Create a new buffInstance from the base buff stats
        var buffinst = BuffInstance.CreateFromSO(_buff); 

        // Add upgrades to the buff which we take FROM the ability instance, this is why we need the source
        buffinst.ApplyAbilityInstanceModifiers(source);
        // I dont understand this still doesn't work!!! Why is this so complicated, surelly there is a better way of doing this!?
        player.PlayerAbilities.GetAbilityInstance(_targetAbility.ID).TriggerBuff(buffinst); // Adds buff to MINING Lazer instance
    }
}