using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static PlayerMovement;
// Handles how the player looks visualy, and also make sure the hitboxes are correct
public class PlayerVisualHandler : NetworkBehaviour, INetworkedPlayerModule {

    private SpriteRenderer sprite; 
    [SerializeField] private SpriteRenderer _bobHand; 
    private Animator animator;
    private string currentAnimation = "";
    public Collider2D playerSwimCollider;
    public Collider2D playerWalkCollider;
    public Light2D lightSpot;
    private float lightStartIntensity;
    private readonly SyncVar<bool> _isFlipped = new SyncVar<bool>(false);

    public int InitializationOrder => 60;

    public void Initialize(NetworkedPlayer playerParent) {
        // Do nothing because it's not client specific
    }
    private void OnEnable() {
        _isFlipped.OnChange += OnFlipChanged;
    }

    private void OnFlipChanged(bool prev, bool next, bool asServer) {
        if (sprite == null) return;
        FlipSprite(next);
    }

    public override void OnStartClient() {
        base.OnStartClient();
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

    // Run on the server, but called by the client
    [ServerRpc(RequireOwnership = true)]
    public void CheckFlipSprite(float horizontalInput) {
        if (horizontalInput > 0.01f) {
            _isFlipped.Value = false; // Flip to right
        } else if (horizontalInput < -0.01f) {
            _isFlipped.Value = true; // Flip to left
        }
    }
    public void FlipSprite(bool shouldFlip) {
        if (shouldFlip) {
            sprite.flipX = true;
            _bobHand.gameObject.transform.parent.localScale = new Vector3(-1, 1, 1);
        } else {             
            sprite.flipX = false;
            _bobHand.gameObject.transform.parent.localScale = new Vector3(1, 1, 1);
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