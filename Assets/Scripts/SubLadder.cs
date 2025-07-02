using DG.Tweening;
using UnityEngine;

public class SubLadder : MonoBehaviour {
    public PlatformEffector2D topPlatform;
    public BoxCollider2D boxColliderPlatform;
    public void SetPlatform(bool enabled) {
        topPlatform.enabled = enabled;
        boxColliderPlatform.enabled = enabled;
    }
}
