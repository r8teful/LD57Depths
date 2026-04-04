using UnityEngine;

// Use this later idk
public class BobHandVisual : MonoBehaviour {
    [SerializeField] private PlayerManager _player;
    [SerializeField] private SpriteRenderer _handRenderer;
    [SerializeField] private Transform _handPivot; // pivot at character's shoulder/hip
    
    void Update() {
        if(_player.PlayerVisuals == null) return;
        //if (_player.PlayerVisuals.IsFlipping) {
        //    HandleFlippingState();
        //    return;
        //}
        if (_player.TryGetCurrentToolDir(out var dir)) {
            AimHandAtMouse(dir); // looks real bad
        }
        //_handRenderer.flipX = !_player.PlayerVisuals.IsFacingRight;
    }

    void AimHandAtMouse(Vector3 dir) {
        dir.z = 0f;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        bool flip = angle > 90f || angle < -90f;

        _handPivot.rotation = Quaternion.Euler(0f, 0f, angle);

        // Usually this is flipY for a hand/arm sprite, but use flipX if your art faces the other way.
        _handRenderer.flipY = flip;
    }

    void HandleFlippingState() {
        // Option A: Hide the hand during the flip (simplest, looks clean)
        _handRenderer.enabled = false;

        
        gameObject.transform.parent.localScale = new Vector3(1, 1, 1);
        // Option B: Snap hand to a "tucked" neutral pose
        // _handPivot.rotation = Quaternion.Euler(0, 0, _player.IsFacingRight ? 0 : 180);
    }
}