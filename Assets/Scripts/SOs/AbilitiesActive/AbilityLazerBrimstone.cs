using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "EffectLazerBrimstone", menuName = "ScriptableObjects/AbilityEffects/Lazer")]
public class AbilityLazerBrimstone : ScriptableObject, IEffectActive {
    
    public void Execute(AbilityInstance source, NetworkedPlayer player) {
        // This could be maybe be like a generic "add buff" effect
    }
}