using System.Collections;
using System.Linq;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
[CreateAssetMenu(fileName = "EffectLazerBrimstone", menuName = "ScriptableObjects/AbilityEffects/Brimstone")]
public class AbilityTriggerBuffTarget : ScriptableObject, IEffectActive, IEffectBuff {
    [SerializeField] AbilitySO _targetAbility;
    [SerializeField] private BuffSO _buff;

    public BuffSO Buff => _buff;
    public AbilitySO Target => _targetAbility;

    public void Execute(AbilityInstance source, NetworkedPlayer player) {
        // This ability should add multiplier buffs to the lazer passive
        Debug.Log("Triggering brimstone buff!");
        var targetInst = player.PlayerAbilities.GetAbilityInstance(_targetAbility.ID);
        if (targetInst == null) {
            Debug.LogError("Coudn't find targetinstance " + targetInst);
            return;
        }
        // Create a new buffInstance from the base buff stats
        var buffinst = BuffInstance.CreateFromSO(_buff);
       
        buffinst.IncreaseBuffPower(source);
        
        targetInst.TriggerBuff(buffinst); 
    }
}