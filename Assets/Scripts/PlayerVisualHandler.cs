using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static PlayerMovement;
// Handles how the player looks visualy, and also make sure the hitboxes are correct
public class PlayerVisualHandler : MonoBehaviour {

    private SpriteRenderer sprite; 
    [SerializeField] private SpriteRenderer _bobHand; 
    private Animator animator;
    private string currentAnimation = "";
    public Collider2D playerSwimCollider;
    public Collider2D playerWalkCollider;
    public Light2D lightSpot;
    private float lightStartIntensity;
    private void Start() {

        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        lightStartIntensity = lightSpot.intensity;
    }
    public void SetHitbox(PlayerState state) {
        switch (state) {
            case PlayerState.None:
                break;
            case PlayerState.Swimming:
                // Horizontal
                playerSwimCollider.enabled = true;
                playerWalkCollider.enabled = false;
                break;
            case PlayerState.Grounded:
                // Vertical
                playerSwimCollider.enabled = false;
                playerWalkCollider.enabled = true;
                break;
            case PlayerState.Cutscene:
                break;
            case PlayerState.ClimbingLadder:
                break;
            default:
                break;
        }
    }
    public void FlipSprite(float horizontalInput) {
        if (horizontalInput > 0.01f) {
            sprite.flipX = false;
            _bobHand.gameObject.transform.parent.localScale = new Vector3(1, 1, 1);
        } else if (horizontalInput < -0.01f) {
            sprite.flipX = true;
            _bobHand.gameObject.transform.parent.localScale = new Vector3(-1, 1, 1);
        }
    }
    public void ChangeAnimation(string animationName) {
        if (animator == null)
            return;
        if (currentAnimation != animationName) {
            currentAnimation = animationName;
            animator.CrossFade(animationName, 0.2f, 0);
            // animator.Play(animationName); // Or use this one instead of crossfade
        }
    }
    public void SetLights(bool setOn) {
        if (setOn) {
            lightSpot.intensity = lightStartIntensity;
        } else {
            lightSpot.intensity = 0;
        }
    }

    public void SetBobHand(bool activateHand) {
        _bobHand.enabled = activateHand;
    }
    
}