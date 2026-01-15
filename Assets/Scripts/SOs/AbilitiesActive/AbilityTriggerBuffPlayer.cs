using System.Collections;
using UnityEngine;
[CreateAssetMenu(fileName = "EffectTriggerBuffPlayer", menuName = "ScriptableObjects/AbilityEffects/Player")]

public class AbilityTriggerBuffPlayer : ScriptableObject, IEffectActive, IEffectBuff {
    [SerializeField] private BuffSO _buff;
    public BuffSO Buff => _buff;
    public AbilitySO Target => null;
    public void Execute(AbilityInstance source, NetworkedPlayer player) {
        var buffinst = BuffInstance.CreateFromSO(_buff);

        buffinst.IncreaseBuffPower(source);

        NetworkedPlayer.LocalInstance.PlayerStats.TriggerBuff(buffinst);
    }
}