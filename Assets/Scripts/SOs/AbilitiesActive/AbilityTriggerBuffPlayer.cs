using UnityEngine;
[CreateAssetMenu(fileName = "EffectTriggerBuffPlayer", menuName = "ScriptableObjects/AbilityEffects/Player")]

public class AbilityTriggerBuffPlayer : ScriptableObject, IEffectActive, IEffectBuff {
    [SerializeField] private BuffSO _buff;
    public BuffSO Buff => _buff;

    public void Execute(AbilityInstance source, PlayerManager player) {
        var buffinst = BuffInstance.CreateFromSO(_buff);

        buffinst.IncreaseBuffPower(source);

        PlayerManager.LocalInstance.PlayerStats.TriggerBuff(buffinst);
    }

    public float GetEffectiveStat(StatType stat, StatModifier tempMod = null) {
        return PlayerManager.LocalInstance.PlayerStats.GetStat(stat);
    }
}