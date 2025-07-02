using DG.Tweening;
using UnityEngine;

public class SubLadder : MonoBehaviour {
    public PlatformEffector2D topPlatform;
    public BoxCollider2D boxColliderPlatform;
    public bool CanUse => gameObject.GetComponent<FixableEntity>() == null; // if there is a fixable component it means its broken 

    public void SetPlatform(bool enabled) {
        topPlatform.enabled = enabled;
        boxColliderPlatform.enabled = enabled;
    }
}
