using UnityEngine;

public class SubLadder : MonoBehaviour {
    public PlatformEffector2D topPlatform;
    public BoxCollider2D boxColliderPlatform;
    public bool CanUse => IsFixed(); 

    public void SetPlatform(bool enabled) {
        topPlatform.enabled = enabled;
        boxColliderPlatform.enabled = enabled;
    }
    private bool IsFixed() {
        if (gameObject.TryGetComponent<FixableEntity>(out var fixEnt)) {
            return fixEnt.IsFixed;
        } else {
            return false;
        }
    }
}
