using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static PlayerMovement;
// Handles how the player looks visualy, and also make sure the hitboxes are correct
public class PlayerVisualHandler : MonoBehaviour, IPlayerModule {
    // We need to rework this to make it easier for us to add costumes and stuff, needs to work with animations, and going into
    // submarine
    [SerializeField] private SpriteRenderer sprite; 
    [SerializeField] private SpriteRenderer _bobHand; 
    [SerializeField] private Sprite _bobHandNormal; 
    [SerializeField] private Sprite _bobHandCactus; 
    [SerializeField] private BobBackVisual _bobBackHandler; 
    [SerializeField] private Transform _bobBackVisual;
    [SerializeField] private Transform _lazerStartPosition;
    [SerializeField] private Animator animator;
    private bool _hasFlippers;
    private string currentAnimation = "";
    public Collider2D playerSwimCollider;
    public Collider2D playerWalkCollider;
    public Light2D lightSpot;
    public PlayerState _state;
    private PlayerManager _localPlayer; 
    private float lightIntensityOn;
    public event Action<bool> OnFlipChange;
    public int InitializationOrder => 2;
    private bool hasInitializedNonOwner; // Sometimes the init function gets called twice so this is just for that
    private bool _isGodMode;
    private bool _isFacingRight = false;
    private bool _shouldFlipNext;

    private bool HasCactus => _localPlayer.PlayerAbilities.HasAbility(ResourceSystem.CactusAbilityID);

    public bool IsFacingRight =>  _isFacingRight;
    public bool IsFlipping { get; internal set; }

    public void InitializeOnOwner(PlayerManager playerParent) {
        lightIntensityOn = lightSpot.intensity;
        _bobBackHandler.SetVisualNone();
        _localPlayer = playerParent;
    }

    // Called every frame, handles approriate visuals depending on each state
    public void HandleVisualUpdate(PlayerState currentState, Vector2 currentInput) {
        switch (currentState) {
            case PlayerState.None:
                break;
            case PlayerState.Swimming:
                HandleSwimVisual(currentInput);
                break;
            case PlayerState.Grounded:
                HandleGroundVisual(currentInput);
                break;
            default:
                break;
        }
    }

    private void HandleGroundVisual(Vector2 currentInput) {
        CheckFlipSprite(currentInput.x);
        ChangeAnimation(Mathf.Abs(currentInput.x) > 0.01f ? "Walk" : "Idle");
    }


    private void HandleSwimVisual(Vector2 currentInput) {
        CheckFlipSprite(currentInput.x);
        if (currentInput.magnitude != 0) {
            // swimming
            animator.speed = 1;
        } else {
            // slower swimming
            animator.speed = 0.4f;            
        }
        SetHandSprite();
    }

    private void SetHandSprite() {
        // This is so uggly and we need a better way to handle visuals but this works
        _bobHand.sprite = HasCactus ? _bobHandCactus : _bobHandNormal;
    }

    internal void OnStateEnter(PlayerState state) {
        _state = state;
        SetHitbox(state);
        switch (state) {
            case PlayerState.Swimming:
                SetLights(true);
                SetBobHand(true);
                ChangeAnimation("Swim");
                _bobBackHandler.SetSpriteSwim();
                break;
            case PlayerState.Grounded:
                SetLights(false);
                SetBobHand(false);
                _bobBackHandler.SetSpriteWalk();
                break;
            case PlayerState.None:
                break;
            default:
                break;
        }
    }
    private void CheckBackVisualTool(bool isStartUsingTool, bool isRemote) {
        return;
        /*
        var tool = isRemote ? _remotePlayer.ToolController.GetCurrentTool(isRemote) : 
                              _localPlayer.ToolController.GetCurrentTool(isRemote);
        if (isStartUsingTool) {
            // tool enabled
            if (tool.BackSprites.Item1 != null) {
                _bobBackHandler.OnToolUseStart();
            }
        } else {
            // Tool disabled
            if (tool.BackSprites.Item1 != null) {
                _bobBackHandler.OnToolUseStop();
            }
        }
         */
    }
    // Such a wierd function but basically when a tool that goes on the back is initialized, this function is called, so we can set the back visual approprietly 
    public void OnToolInitBack(IToolVisual toolVisual) {
        _bobBackHandler.HandleStartup(toolVisual);
    }

    public void SetHitbox(PlayerState state) {
        switch (state) {
            case PlayerState.None:
                break;
            case PlayerState.Swimming:
                // Horizontal
                if (!_isGodMode) 
                    playerSwimCollider.enabled = true;                
                playerWalkCollider.enabled = false;
                break;
            case PlayerState.Grounded:
                // Vertical
                playerSwimCollider.enabled = false;
                playerWalkCollider.enabled = true;
                break;
            default:
                break;
        }
    }

    public void CheckFlipSprite(float horizontalInput) {
        var flippNow = _isFacingRight;
        if (horizontalInput > 0.01f) {
            _isFacingRight = false; // Flip to right
        } else if (horizontalInput < -0.01f) {
            _isFacingRight = true; // Flip to left
        }
        if (flippNow != _isFacingRight) {
            if(_state == PlayerState.Swimming) {
                FlipPlayerAnimation(_isFacingRight);
            } else {
                FlipNow(true, _isFacingRight);
            }
        }

    }
    // called from the animator clip event call
    public void OnShouldFlip() {
        FlipNow(); // will use _shouldFlipNext
      }
    private void FlipNow(bool useFlipOverride = false, bool flipOverwride = false) {
        IsFlipping = false;
        bool shouldFlip = useFlipOverride ? flipOverwride : _shouldFlipNext;
        if (shouldFlip) {
            GetComponent<SpriteRenderer>().flipX = true;
            sprite.flipX = true;
            _bobHand.gameObject.transform.parent.localScale = new Vector3(-1, 1, 1);
            _bobBackVisual.localScale = new Vector3(-1, 1, 1);
            playerSwimCollider.transform.localScale = new Vector3(-1, 1, 1);
        } else {
            sprite.flipX = false;
            GetComponent<SpriteRenderer>().flipX = false;
            _bobHand.gameObject.transform.parent.localScale = new Vector3(1, 1, 1);
            _bobBackVisual.localScale = new Vector3(1, 1, 1);
            playerSwimCollider.transform.localScale = new Vector3(1, 1, 1);

        }
        OnFlipChange?.Invoke(shouldFlip); // Now we actually call it because we've flipped (animation give it a delay)
                                               //ChangeAnimation(_nextAnimationAfterFlip);

    }
    private void FlipPlayerAnimation(bool shouldFlip) {
        _shouldFlipNext = shouldFlip;
        IsFlipping = true;
        if (shouldFlip)
            ChangeAnimation("SwimFlip");
        else
            ChangeAnimation("SwimFlipReverse");

        return;
    }

    private void ChangeBackSprite() {

    }
    private void ChangeAnimation(string animationName) {
        if (animator == null)
            return;
        if (currentAnimation != animationName) {
            currentAnimation = animationName;
            //animator.CrossFade(animationName, 0.2f,0);
            animator.Play(animationName); // Or use this one instead of crossfade
        }
    }
    private void SetLights(bool setOn) {
        if (setOn) {
            if (lightIntensityOn <= 0) {
                lightIntensityOn = 1; // 
            }
            lightSpot.intensity = lightIntensityOn;
        } else {
            lightSpot.intensity = 0;
        }
    }

    private void SetBobHand(bool activateHand) {
        _bobHand.enabled = activateHand;
    }
    /*
    This is so fucked we need to rethink it because I'm just going in circles and there must be a better structure for this to be less fucking
    buggy. The visuals should be shown when: 
    
    For local visual: Only show when we are actually using a tool
    For remote visual: Show when they are using the tool & we have vibility over them
    
    We can only use a tool when we are swimming, if we are using a tool and go out of swimming mode, the tool should stop

    That's it. That is all the logic. We just need an easy variable that is "Can we see client X" which pretty much just boils down to if we are 
    on the same layer and internal ID
    */ 
    public void OnStartDrilling() {
        CheckBackVisualTool(true,false); // Update back visual for local player 
        SetBobHand(false);
    }


    public void OnStopDrilling() {
        if(_localPlayer.PlayerLayerController.CurrentLayer == VisibilityLayerType.Interior) {
            return; // Don't enable the hand if we are in the interior
        }
        CheckBackVisualTool(false,false); // Update back visual for local player 
        SetBobHand(true);
    }
    public void HandleBobLayerChange(bool shouldBeVisible, VisibilityLayerType layer) {
        SetComponentsActiveRecursive<Renderer>(shouldBeVisible);
        //foreach (Renderer r in GetComponentsInChildren<Renderer>(true)) // Include inactive children
        //    r.enabled = shouldBeVisible;
        //foreach (Collider2D c in GetComponentsInChildren<Collider2D>(true)) {
        //    c.enabled = shouldBeVisible;
        //}
        if (shouldBeVisible) {
            // Only really makes sence to change the visuals if we are visible otherwise it might cause wierd visual bugs
            switch (layer) {
                case VisibilityLayerType.Exterior:
                    OnStateEnter(PlayerState.Swimming);
                    break;
                case VisibilityLayerType.Interior:
                    OnStateEnter(PlayerState.Grounded);
                    break;
                default:
                    break;
            }
        }
    } // Generic recursive component activation/deactivation helper
    private void SetComponentsActiveRecursive<T>(bool isActive) where T : Component {
        // Find components ONLY within the target object and its children
        T[] components = GetComponentsInChildren<T>(true); // include inactive ones
        foreach (T component in components) {
            // Skip if this or any parent has PreserveComponentToggle
            if (component.GetComponentInParent<PreserveVisibility>() != null)
                continue;
            // Enable/disable based on the component type's relevant property
            if (component is Behaviour behaviour)
                behaviour.enabled = isActive;
            else if (component is Renderer renderer)
                renderer.enabled = isActive;
            else if (component is Collider2D collider) {
                if (!collider.isTrigger)
                    collider.enabled = isActive;
            }
            // Add more types if necessary (Light, ParticleSystem, etc.)
        }
    }

    internal void DEBUGSetGodMode(bool v) {
        playerSwimCollider.enabled = !v;
        _isGodMode = v;
    }

    internal Vector3 GetLazerPos() {
        return _lazerStartPosition.position;
    }
}