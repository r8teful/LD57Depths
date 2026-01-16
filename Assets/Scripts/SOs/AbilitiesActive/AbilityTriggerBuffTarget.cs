using UnityEngine;
[CreateAssetMenu(fileName = "EffectLazerBrimstone", menuName = "ScriptableObjects/AbilityEffects/Brimstone")]
public class AbilityTriggerBuffTarget : ScriptableObject, IEffectActive, IEffectBuff {
    [SerializeField] AbilitySO _targetAbility;
    [SerializeField] private BuffSO _buff;

    public BuffSO Buff => _buff;

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

    public float GetEffectiveStat(StatType stat, StatModifier tempMod = null) {
        var id = _targetAbility.ID;
        var ab = NetworkedPlayer.LocalInstance.PlayerAbilities.GetAbilityInstance(id);
        return ab.GetEffectiveStat(stat, tempMod);
    }
}