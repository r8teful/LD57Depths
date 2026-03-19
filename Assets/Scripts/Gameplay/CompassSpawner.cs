using System.Collections;
using UnityEngine;

public class CompassSpawner : MonoBehaviour, IInitializableAbility {
    public bool IsPlus;
    public void Init(AbilityInstance instance, PlayerManager player) {
        if (UIManager.Instance != null) {
            if (IsPlus) {
                UIManager.Instance.PlayerHUD.Compass.ActivateCompassPlus(); // lol

            }else {
                UIManager.Instance.PlayerHUD.Compass.ActivateNormal(player);

            }
        }
    }
}