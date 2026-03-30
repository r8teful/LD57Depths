using UnityEngine;

public class AdrenalinePlus : MonoBehaviour, IInitializableAbility {
    public AbilitySO AdrenalineAbility;
    public void Init(AbilityInstance instance, PlayerManager player) {
        var ability = player.PlayerAbilities.GetAbilityInstance(AdrenalineAbility.ID);
        if (ability == null || ability.Object == null || ability.Object.TryGetComponent<Adrenaline>(out var adrenaline)) {
            Debug.LogError("Coudn't find adrenaline script! " + ability);
            return;
        }
        adrenaline.ActivePlus();
       
    }
}