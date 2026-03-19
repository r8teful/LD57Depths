using System.Collections;
using UnityEngine;

public class CompassSpawner : MonoBehaviour, IInitializableAbility {
    public void Init(AbilityInstance instance, PlayerManager player) {
        if (UIManager.Instance != null) { 
            UIManager.Instance.PlayerHUD.Compass.Activate(player);
        }
    }
}