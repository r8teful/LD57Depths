using UnityEngine;

// the lazer is a passive because we can always use it, it doesn't have any delays or anything...

[CreateAssetMenu(fileName = "EffectLazer", menuName = "ScriptableObjects/AbilityEffects/Lazer")]
public class EffectLazer : ScriptableObject, IEffectPassive {
    [SerializeField] GameObject lazerPrefab;


    public void Apply(AbilityInstance instance, NetworkedPlayer player) {
        // Spawn the monobehaviour that will handle the lazer stuff for us!
        var g = Instantiate(lazerPrefab, player.PlayerAbilities.AbilitySlot);
        g.GetComponent<MiningLazerNew>().Init(instance, player);
    }

    public void Remove(AbilityInstance instance, NetworkedPlayer player) {
        Debug.Log($"todo!");
    }
}