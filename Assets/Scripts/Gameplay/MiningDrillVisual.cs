using System.Collections;
using UnityEngine;

public class MiningDrillVisual : MonoBehaviour, IToolVisual {
    [SerializeField] private SpriteRenderer _sprite;
    public void HandleVisualStart() {
        // Show the drill
        _sprite.color = Color.black;
        // So this up here is not working, it should show up on the remote client but I don't know why it isn't
        // We should start by looking if we actually listen to the onchange in the start. It must be somewhere there
    }
    public void HandleVisualStop() {
        // Hide the drill
        _sprite.color = Color.white;
    }

    public void HandleVisualUpdate(InputManager inputManager) {
        DrillVisual(inputManager.GetAimInput());
    }

    public void Init(IToolBehaviour parent) {
        // Don't need any special visuals atm
    }

    private void DrillVisual(Vector2 pos) {
        Vector2 objectPos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 directionToMouse = (pos - objectPos2D).normalized;
        float angle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}