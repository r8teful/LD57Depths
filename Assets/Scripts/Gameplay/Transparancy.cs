using System.Collections;
using UnityEngine;

public class Transparancy : MonoBehaviour, IInitializableAbility {
    public void Init(AbilityInstance instance, NetworkedPlayer player) {
        Debug.Log("Transparancy suit equiped!");
    }
    private void OnDestroy() {
        Debug.Log("Transparancy GONE!");
    }
}