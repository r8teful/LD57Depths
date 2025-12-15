using System.Collections;
using UnityEngine;
using System.Linq;
[CreateAssetMenu(fileName = "EffectLazerBrimstone", menuName = "ScriptableObjects/AbilityEffects/Brimstone")]
public class AbilityLazerBrimstone : ScriptableObject, IEffectActive, IEffectBuff {
    [SerializeField] AbilitySO _lazerAbilityTarget;
    [SerializeField] private BuffSO _buff;

    public BuffSO Buff => _buff;

    public void Execute(AbilityInstance source, NetworkedPlayer player) {
        // This ability should add multiplier buffs to the lazer passive
        Debug.Log("Triggering brimstone buff!");
        var buffinst = BuffInstance.CreateFromSO(_buff);
        buffinst.ApplyAbilityInstanceModifiers(source);
        player.PlayerAbilities.GetInstance(_lazerAbilityTarget.ID).TriggerBuff(buffinst);
    }
}