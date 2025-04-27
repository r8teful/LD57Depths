using Sirenix.OdinInspector;
using UnityEngine;

public class DEBUGManager : MonoBehaviour
{
    [SerializeField] PlayerController player;
    [OnValueChanged("PlayerSpeed")]
    public float playerSpeed;
    private void PlayerSpeed() {
        player.accelerationForce = playerSpeed;
        player.swimSpeed = playerSpeed;
    }
}
