using UnityEngine;

public class CactusSuit : MonoBehaviour, IInitializableAbility {

    public void Init(AbilityInstance instance, NetworkedPlayer player) {
        Debug.Log("Cactus suit equiped!");
    }
    private void OnDestroy() {
        Debug.Log("Cactus suit GONE!");
    }
}