using UnityEngine;

public class MiningFishVisual : MonoBehaviour
{
    private PlayerManager _player;

    internal void HandleVisualUpdate() {
        Vector2 toolPosition = transform.position;
        Vector2 pos = _player.InputManager.GetAimWorldInput(transform);
        Vector2 targetDirection = (pos - toolPosition).normalized;
        float angle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0,0,angle);
        ScaleFlip(angle);
    }

    private void ScaleFlip(float angle) {
        Vector3 scale = transform.localScale;
        if(angle > 90 || angle < -90) {
            // aimed left
            scale.y = -Mathf.Abs(scale.y);
        } else {
            scale.y = Mathf.Abs(scale.y);
        }
        transform.localScale = scale;
    }

    internal void Init(PlayerManager player) {
        _player = player;
    }
}