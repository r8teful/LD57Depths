using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using UnityEngine;

// Controls what happens when the player presses the left mouse button, which usually will activate a specific tool
public class ToolController : NetworkBehaviour, INetworkedPlayerModule {
    [SerializeField] private Transform _toolSlotMining;

    // local owner only!
    private bool _isUsingToolLocal;
    private NetworkedPlayer _playerParent; 
    private List<IToolBehaviour> _toolBehaviours = new List<IToolBehaviour>();
    private List<IToolVisual> _toolVisuals = new List<IToolVisual>();
    private List<GameObject> _toolInstances = new List<GameObject>();
    public NetworkedPlayer GetPlayerParent() => _playerParent;
    public IToolBehaviour CurrentToolBehaviour => _toolBehaviours.Count > _currentToolID.Value ? _toolBehaviours[_currentToolID.Value] : null;
    private IToolVisual CurrentToolVisual => _toolVisuals.Count > _currentToolID.Value ? _toolVisuals[_currentToolID.Value] : null;

    private float _inputSendTimer;

    // Server
    private WorldManager _worldManager;
    private readonly SyncVar<ushort> _currentToolID = new SyncVar<ushort>();
    private readonly SyncVar<bool> _isUsingTool = new SyncVar<bool>(false);
    private readonly SyncVar<Vector2> _input = new SyncVar<Vector2>(Vector2.zero,new SyncTypeSettings(0.4f));
    public SyncVar<ushort> CurrentToolID => _currentToolID;
    public SyncVar<bool> IsUsingTool => _isUsingTool;
    public SyncVar<Vector2> Input => _input;

    public int InitializationOrder => 92;

    public void StartClient(bool isOwner, NetworkedPlayer owner) {
        InstantiateTools(owner);
        SetToolVisualState(0, true); // Enable first one, ID will depend on what client has selected 
        //SetToolUseVisualState(_isUsingTool.Value);
        _isUsingTool.OnChange += OnIsUsingToolChanged;
        _currentToolID.OnChange += OnToolChange;
        _input.OnChange += OnInputChange;
        owner.PlayerVisuals.OnFlipChange += PlayerVisuals_OnFlipChange;
    }

    private void PlayerVisuals_OnFlipChange(bool isFlipped) {
        if (IsOwner) return; // Only care if we are remote
        CurrentToolVisual?.FlipVisual(isFlipped);
    }

    private void OnInputChange(Vector2 prev, Vector2 next, bool asServer) {
        if (IsOwner) return; // Only care if we are remote
        CurrentToolVisual?.UpdateVisual(next);
    }

    private void OnIsUsingToolChanged(bool prev, bool next, bool asServer) {
        if (IsOwner) return; // Only care if we are remote
        SetToolUseVisualState(next);
    }

    private void InstantiateTools(NetworkedPlayer owningPlayer) {
        foreach (var tool in App.ResourceSystem.GetAllTools()) {
            if (tool == null) continue;

            GameObject toolInstance = Instantiate(tool, _toolSlotMining);
            _toolInstances.Add(toolInstance);

            IToolVisual visual = toolInstance.GetComponent<IToolVisual>();
            visual?.Init(base.IsOwner, owningPlayer);
            _toolVisuals.Add(visual);

            if (base.IsOwner) {
                IToolBehaviour behaviour = toolInstance.GetComponent<IToolBehaviour>();
                behaviour?.Init(owningPlayer);
                _toolBehaviours.Add(behaviour);
            }

            toolInstance.SetActive(false);
        }
    }
    private void OnToolChange(ushort prev, ushort next, bool asServer) {
        SetToolVisualState(prev, false);
        SetToolVisualState(next, true);
    }

    public void InitializeOnOwner(NetworkedPlayer playerParent) {
        _playerParent = playerParent;
        Console.RegisterCommand(this, "DEBUGSetMineTool", "setMineTool", "god");
        _worldManager = FindFirstObjectByType<WorldManager>();
        _playerParent.PlayerLayerController.CurrentLayer.OnChange += PlayerLayerChange;
        StartClient(true, playerParent);
    }
    public void InitalizeNotOwner(NetworkedPlayer playerParent) {
        StartClient(false, playerParent);
    }

    // The ToolController now handles updating, this is basically just pulling what we had in MiningBase but doing it here
    // So we have one place for it and it not everywhere and confusing and hard to follow 
    private void Update() {
        if (!base.IsOwner || !_isUsingToolLocal || !_playerParent.IsInitialized) {
            return;
        }
        //var curAimTarget = _playerParent.InputManager.GetAimWorldInput();
        var curAimTarget = _playerParent.InputManager.GetDirFromPos(transform.position); // This should work, because the toolController is on the player
        // We update local visuals every frame for smooth movement
        CurrentToolBehaviour?.OwnerUpdate();
        var visualData = CurrentToolBehaviour?.VisualData;
        if (visualData != null) {
            CurrentToolVisual?.UpdateVisual(visualData,_playerParent.InputManager);
        }
        _inputSendTimer += Time.deltaTime;
        if (_inputSendTimer >= 0.4f) {
            _inputSendTimer = 0f;
            ToolInputServerRpc(curAimTarget);
        }
    }

    private void PlayerLayerChange(VisibilityLayerType prev, VisibilityLayerType next, bool asServer) {
        if (_isUsingToolLocal && next==VisibilityLayerType.Interior) {
            // Stop using the tool
            ToolStop();
        }
    }

    public override void OnStartServer() {
        base.OnStartServer();
        _worldManager = FindFirstObjectByType<WorldManager>();
        //if (App.isEditor) {
        //    DEBUGSetMineTool("drill");
        //    DEBUGSetDefaultCleanTool();
        //}
    }
    public void ToolStart(InputManager input) {
        // Don't think we have to check this, because its called from the input manager, which will only be called by the owner
        if (!base.IsOwner || _isUsingToolLocal) // Dont start if already started
            return;
        // Is this action allowed?
        if (!_playerParent.PlayerMovement.CanUseTool())
            return;
        _isUsingToolLocal = true;
        // Start BEHAVIOUR (which starts its own low-frequency coroutine)
        CurrentToolBehaviour?.ToolStart(input, this);
        // Start VISUALS
        CurrentToolVisual?.StartVisual();
        ToolStartServerRpc(CurrentToolBehaviour.ToolID);
    }
 
    public void ToolStop() {
        if (!base.IsOwner || !_isUsingToolLocal) // Don't stop if already stopped
            return;
        // Probbly still want to end it even though if we can't
        //if (!_playerParent.PlayerMovement.CanUseTool())
        //    return;

        _isUsingToolLocal = false;
        // Stop BEHAVIOUR (which stops its coroutine)
        CurrentToolBehaviour?.ToolStop(this);

        // Stop VISUALS
        CurrentToolVisual?.StopVisual();
        ToolStopServerRpc(); // Let others know we've stopped 
    }

    private void SetToolUseVisualState(bool isUsing) {
        if (isUsing) {
            CurrentToolVisual?.StartVisual();
            // We got a new "is using" state, let's immediately snap the visual to the last known network position.
            // The tool's internal interpolation will take over from here.
            CurrentToolVisual?.UpdateVisual(_input.Value);
        } else {
            CurrentToolVisual?.StopVisual();
        }
    }

    private void SetToolVisualState(ushort toolID, bool active) {
        if (toolID >= _toolInstances.Count) return;
        GameObject instance = _toolInstances[toolID];
        if (instance != null && (instance.activeSelf != active)) {
            // Only set active if we aren't in that state already
            instance.SetActive(active);
        }
    }
    #region Networking

    /*
* These two functions are called by the owning player, then, in PlayerVisualHandler attached to this playerObject on the 
* remove client will recieve an OnChangeEvent saying "This player just started/stopped using their tool!" and then they
* will locally simulate that tools visuals
*/
    [ServerRpc]
    private void ToolStartServerRpc(ushort ID) {
        _currentToolID.Value = ID; // Gues we don't have to set this value all the time, probably just when switching, but like this is easy
        _isUsingTool.Value = true;
    }
    [ServerRpc]
    private void ToolStopServerRpc() {
        _isUsingTool.Value = false;
    }

    [ServerRpc]
    private void ToolInputServerRpc(Vector2 input) {
        _input.Value = input;
    }
    [ServerRpc]
    private void ToolChangeServerRpc(ushort ID) {
        _currentToolID.Value = ID;
    }

    [ServerRpc(RequireOwnership = true)]
    public void CmdRequestDamageTile(Vector3 worldPos, float damageAmount) {
        // TODO: Server-side validation (range, tool, cooldowns, etc.)
        //Debug.Log($"Requesting damage worldPos {worldPos}, damageAmount {damageAmount}");
        // Pass request to WorldGenerator for processing
        // TODO somehow _worldmanager is null here and it cant find it 
        if (_worldManager == null)
            _worldManager = FindFirstObjectByType<WorldManager>();
        _worldManager.RequestDamageTile(worldPos, damageAmount);
    }
    [ServerRpc(RequireOwnership = true)]
    public void CmdRequestDamageTile(Vector3Int cellPos, short damageAmount) {
        if (_worldManager == null)
            _worldManager = FindFirstObjectByType<WorldManager>();
        _worldManager.RequestDamageTile(cellPos, damageAmount);
    }
    #endregion

    private void DEBUGSetMineTool(string tool) {
        Debug.Log("CALLED");
        if (tool == "lazer" || tool == "laser") {
            ToolChangeServerRpc(0);
        }
        if (tool == "dril" || tool == "drill") {
            ToolChangeServerRpc(1);
        }
        if (tool == "RPG" || tool == "rpg" || tool == "launcher" || tool == "rocket") {
            ToolChangeServerRpc(2);
        }
    }
}