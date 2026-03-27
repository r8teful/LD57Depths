using System.Collections;
using UnityEngine;

// Use this later idk
public class BobHandVisual : MonoBehaviour {
    [SerializeField] private PlayerVisualHandler _player;
    [SerializeField] private SpriteRenderer _handRenderer;
    [SerializeField] private Transform _handPivot; // pivot at character's shoulder/hip

    void Update() {
        if (_player.IsFlipping) {
            HandleFlippingState();
            return;
        }

        AimHandAtMouse();
        _handRenderer.flipX = !_player.IsFacingRight;
    }

    void AimHandAtMouse() {
        // Get aim direction in world space
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 dir = (mouseWorld - _handPivot.position);
        dir.z = 0;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // If facing left, flip the angle so the arm doesn't cross the body
        if (!_player.IsFacingRight)
            angle += 180f;

        _handPivot.rotation = Quaternion.Euler(0, 0, angle);
    }

    void HandleFlippingState() {
        // Option A: Hide the hand during the flip (simplest, looks clean)
        _handRenderer.enabled = false;

        
        gameObject.transform.parent.localScale = new Vector3(1, 1, 1);
        // Option B: Snap hand to a "tucked" neutral pose
        // _handPivot.rotation = Quaternion.Euler(0, 0, _player.IsFacingRight ? 0 : 180);
    }
}