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

        // Create a new buffInstance from the base stats
        var buffinst = BuffInstance.CreateFromSO(_buff); // This is fucked, makes 1.5dmg to 3.5 somehow, we simply need to actually add the modifiers properly

        // Add upgrades to the buff which we take FROM the ability instance, this is why we need the source
        // IMPORTANT: we need to make sure that we ADD these to our buffinstance, we don't just replace, because the modifiers ADD
        // to the base of the buff, this is the whole reason the buffs become stronger
        buffinst.ApplyAbilityInstanceModifiers(source);
        // I dont understand this still doesn't work!!! Why is this so complicated, surelly there is a better way of doing this!?
        player.PlayerAbilities.GetAbilityInstance(_targetAbility.ID).TriggerBuff(buffinst); // Adds buff to MINING Lazer instance
    }
}