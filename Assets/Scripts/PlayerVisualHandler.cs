using FishNet.Component.Animating;
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
    private NetworkAnimator animatorNetwork;
    private string currentAnimation = "";
    public Collider2D playerSwimCollider;
    public Collider2D playerWalkCollider;
    public Light2D lightSpot;
    public NetworkedPlayer _remotePlayer;
    private float lightIntensityOn;
    private readonly SyncVar<bool> _isFlipped = new SyncVar<bool>(false);
    public int InitializationOrder => 60;
    private bool hasInitializedNonOwner; // Sometimes the init function gets called twice so this is just for that

    public void InitializeOnOwner(NetworkedPlayer playerParent) {
        InitCommon();
    }
    private void OnEnable() {
        _isFlipped.OnChange += OnFlipChanged;
    }

    private void OnFlipChanged(bool prev, bool next, bool asServer) {
        if (sprite == null) return;
        FlipSprite(next);
    }
    private void InitCommon() {
        animator = GetComponent<Animator>();
        animatorNetwork = GetComponent<NetworkAnimator>();
        sprite = GetComponent<SpriteRenderer>();
        lightIntensityOn = lightSpot.intensity;
    }
    public void InitializeOnNotOwner() {
        if (hasInitializedNonOwner)
            return;
        InitCommon();
        Debug.Log("setting up remote player on non owning client!");
        // Set remote client specific visuals
        // Cache remotclient networkedPlayer script.
        if (NetworkedPlayersManager.Instance.TryGetPlayer(base.OwnerId, out NetworkedPlayer remoteClient)) {
            _remotePlayer = remoteClient;
        } else {
            Debug.LogError("Could not find networkedPlayer on remote client!");
            return;
        }
        HandleRemoteToolSetup();
        hasInitializedNonOwner = true;
    }

    // For remote remote client, tools should be generic, first know which tool they are using, then enable it, then sync it and have the specific tool
    // Logic handle the rest
    public void HandleRemoteToolSetup() {
        // We have to subscribe to the onchange on the toolController so we can know when to enable/disable the tools
        _remotePlayer.ToolController.IsUsingTool.OnChange += RemoteClientToolChange;
        _remotePlayer.ToolController.Input.OnChange += RemoteClientInputChange;
        _remotePlayer.ToolController.EquipAllToolsVisualOnly();

        /* What we want is the following:
        - Know WHICH tool they are using over the network
        - Know WHEN that tool is being used
        - Know WHERE they are aiming that tool
         */
    }

    private void RemoteClientInputChange(Vector2 prev, Vector2 next, bool asServer) {
        Debug.Log("RemoteClient input is now: " + next);
        _remotePlayer.ToolController.GetCurrentTool().HandleVisualUpdateRemote(next);
    }

    private void RemoteClientToolChange(bool prev, bool next, bool asServer) {
        if (next) {
            // tool enabled
            _remotePlayer.ToolController.GetCurrentTool().HandleVisualStart(this);
        } else {
            // Tool disabled
            _remotePlayer.ToolController.GetCurrentTool().HandleVisualStop(this);
        }
        // TODO then somehow we would need to set what input they have and communicate it over the network, Thats about it
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
            animator.CrossFade(animationName, 0.2f,0);
            animatorNetwork.CrossFade(animationName, 0.2f, 0);
            // animator.Play(animationName); // Or use this one instead of crossfade
        }
    }
    public void SetLights(bool setOn) {
        if (setOn) {
            if (lightIntensityOn <= 0) {
                lightIntensityOn = 1; // 
            }
            lightSpot.intensity = lightIntensityOn;
        } else {
            lightSpot.intensity = 0;
        }
    }

    public void SetBobHand(bool activateHand) {
        _bobHand.enabled = activateHand;
    }
  
}