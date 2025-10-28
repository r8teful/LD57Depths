using FishNet.Component.Animating;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static PlayerMovement;
// Handles how the player looks visualy, and also make sure the hitboxes are correct
public class PlayerVisualHandler : NetworkBehaviour, INetworkedPlayerModule {

    private SpriteRenderer sprite; 
    [SerializeField] private SpriteRenderer _bobHand; 
    [SerializeField] private BobBackVisual _bobBackHandler; 
    [SerializeField] private Transform _bobBackVisual; 
    private Animator animator;
    private NetworkAnimator animatorNetwork;
    private bool _hasFlippers;
    private string currentAnimation = "";
    public Collider2D playerSwimCollider;
    public Collider2D playerWalkCollider;
    public Light2D lightSpot;
    // There could every only be two references for a player here, we are either owner, which would be the localPlayer reference
    // Or we are not the owner, in which case it would be a remote.
    public NetworkedPlayer _remotePlayer;
    public NetworkedPlayer _localPlayer;
    private float lightIntensityOn;
    private readonly SyncVar<bool> _isFlipped = new SyncVar<bool>(false);
    public int InitializationOrder => 2;
    private bool hasInitializedNonOwner; // Sometimes the init function gets called twice so this is just for that

    public void InitializeOnOwner(NetworkedPlayer playerParent) {
        InitCommon();
        _localPlayer = playerParent;
        playerParent.UpgradeManager.OnUpgradePurchased += OnPlayerUpgradePurchased;
    }

    private void OnPlayerUpgradePurchased(UpgradeRecipeSO upgrade) {
        // This works now, this will peace of code will now run on the client who purchased a specific upgrade
        // Here you would change sprites etc..
        if (upgrade.ID == ResourceSystem.UpgradeFlippersID) {
            // equip flippers
            _hasFlippers = true;
        }
        _bobBackHandler.HandleUpgradeBought(upgrade);   
    }

    private void OnEnable() {
        _isFlipped.OnChange += OnFlipChanged;
    }
    private void InitCommon() {
        animator = GetComponent<Animator>();
        animatorNetwork = GetComponent<NetworkAnimator>();
        sprite = GetComponent<SpriteRenderer>();
        lightIntensityOn = lightSpot.intensity;
        _bobBackHandler.SetVisualNone();
        // Surelly we have to subscribe to the upgrade purchase event here? For example, I purchase an upgrade. 
        // now two things need to happen:
        // 1. Player visual on MY system needs to recognise it so that it can add the flippers
        // 2. Player visual on all REMOTE systems need to recognise it and add flippers to my character
    }
    public void InitializeOnNotOwner(NetworkedPlayer remoteClient) {
        if (hasInitializedNonOwner)
            return;
        InitCommon();
        _remotePlayer = remoteClient;
        // Subscribe to handle remote stat changes
        remoteClient.PlayerStats.OnStatChanged += OnRemoteStatsChanged;
        remoteClient.UpgradeManager.OnUpgradePurchased += OnPlayerUpgradePurchased;
        HandleRemoteToolSetup(_remotePlayer);
        hasInitializedNonOwner = true;
    }

    private void OnRemoteStatsChanged(StatType arg1, float arg2) {
        // In reality, we would only care about the stats that VISUALLY change the remote client, so that those can be shown 

        // Possibly raise an event here but have to make sure we don't mistake it for a LOCAL stat change
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
            case PlayerState.Cutscene:
                HandleSwimVisual(currentInput);
                break;
            case PlayerState.ClimbingLadder:
                HandleClimbingLadderVisual(currentInput);
                break;
            default:
                break;
        }
    }

    private void HandleClimbingLadderVisual(Vector2 currentInput) { 
        if (Mathf.Abs(currentInput.y) > 0.01f) {
            ChangeAnimation("Climb");
        } else {
            ChangeAnimation("ClimbIdle"); // Or just stop animator speed for Climb animation
        }
    }

    private void HandleGroundVisual(Vector2 currentInput) {
        CheckFlipSprite(currentInput.x);
        ChangeAnimation(Mathf.Abs(currentInput.x) > 0.01f ? "Walk" : "Idle");
    }

    private void HandleSwimVisual(Vector2 currentInput) {

        if (currentInput.magnitude != 0) {
            // So ugly, will break if we need more than just flippers on the actual animation but this works
            ChangeAnimation(_hasFlippers ? "SwimFlippers" : "Swim");
        } else {
            ChangeAnimation(_hasFlippers ? "SwimIdleFlippers" : "SwimIdle");
        }
        CheckFlipSprite(currentInput.x);
    }
    internal void OnStateEnter(PlayerState state) {
        SetHitbox(state);
        switch (state) {
            case PlayerState.Swimming:
                SetLights(true);
                SetBobHand(true);
                _bobBackHandler.SetSpriteSwim();
                break;
            case PlayerState.Grounded:
                SetLights(false);
                SetBobHand(false);
                _bobBackHandler.SetSpriteWalk();
                break;
            case PlayerState.Cutscene:
                SetLights(false);
                SetBobHand(false);
                break;
            case PlayerState.ClimbingLadder:
                SetLights(false);
                SetBobHand(false);
                break;
            case PlayerState.None:
                break;
            default:
                break;
        }
    }

    // For remote remote client, tools should be generic, first know which tool they are using, then enable it, then sync it and have the specific tool
    // Logic handle the rest
    public void HandleRemoteToolSetup(NetworkedPlayer remotePlayer) {
        // We have to subscribe to the onchange on the toolController so we can know when to enable/disable the tools
        _remotePlayer.ToolController.IsUsingTool.OnChange += RemoteClientToolIsUningChange;
        _remotePlayer.ToolController.Input.OnChange += RemoteClientInputChange;
        _remotePlayer.ToolController.EquipAllToolsVisualOnly(remotePlayer);

        /* What we want is the following:
        - Know WHICH tool they are using over the network
        - Know WHEN that tool is being used
        - Know WHERE they are aiming that tool
         */
    }

    private void RemoteClientInputChange(Vector2 prev, Vector2 next, bool asServer) {
        Debug.Log("RemoteClient input is now: " + next);
        _remotePlayer.ToolController.GetCurrentTool(true).HandleVisualUpdateRemote(next);
    }

    private void RemoteClientToolIsUningChange(bool prev, bool next, bool asServer) {
        if (!HasVisibility())
            return;
        var tool = _remotePlayer.ToolController.GetCurrentTool(true);
        if (next) {
            // tool enabled
            tool.HandleVisualStart(this);
        } else {
            // Tool disabled
            tool.HandleVisualStop(this);
        }
        CheckBackVisualTool(next,true); // update back visual for remote player
        // TODO then somehow we would need to set what input they have and communicate it over the network, Thats about it
    }
    private void CheckBackVisualTool(bool isStartUsingTool, bool isRemote) {
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
    private void OnFlipChanged(bool prev, bool next, bool asServer) {
        if (sprite == null)
            return;
        FlipPlayer(next);
    }
    private void FlipPlayer(bool shouldFlip) {
        if (shouldFlip) {
            sprite.flipX = true;
            _bobHand.gameObject.transform.parent.localScale = new Vector3(-1, 1, 1);
            _bobBackVisual.localScale = new Vector3(-1, 1, 1);
        } else {             
            sprite.flipX = false;
            _bobHand.gameObject.transform.parent.localScale = new Vector3(1, 1, 1);
            _bobBackVisual.localScale = new Vector3(1, 1, 1);
        }
    }
    private void ChangeBackSprite() {

    }
    private void ChangeAnimation(string animationName) {
        if (animator == null)
            return;
        if (currentAnimation != animationName) {
            currentAnimation = animationName;
            animator.CrossFade(animationName, 0.2f,0);
            animatorNetwork.CrossFade(animationName, 0.2f, 0);
            // animator.Play(animationName); // Or use this one instead of crossfade
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
        if (!base.IsOwner) {
            if (!HasVisibility()) {
                return; // we are a remote player and don't have vibility, don't do any visual updates 
            }
        }
        CheckBackVisualTool(true,false); // Update back visual for local player 
        SetBobHand(false);
    }

    private bool HasVisibility() {
        if (_localPlayer == null) {
            if (NetworkedPlayersManager.Instance.TryGetPlayer(base.LocalConnection.ClientId, out var localPlayer)) {
                _localPlayer = localPlayer;
            }
        }
        if (_localPlayer == null || _remotePlayer == null) {
            Debug.LogError("Need both local and remote reference to check if a non owner can set visuals properly!");
            return false;
        }
        var localLayer = _localPlayer.PlayerLayerController.CurrentLayer.Value;
        var remoteLayer = _remotePlayer.PlayerLayerController.CurrentLayer.Value;
        // Todo also check layerID
        if (localLayer != remoteLayer) {
            return false;
        }
        return true;
    }

    public void OnStopDrilling() {
        if (!base.IsOwner) {
            if (!HasVisibility()) {
                return; // we are a remote player and don't have vibility, don't do any visual updates 
            }
        }
        if(_localPlayer.PlayerLayerController.CurrentLayer.Value == VisibilityLayerType.Interior) {
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
}