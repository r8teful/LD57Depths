using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

// Controls what happens when the player presses the left mouse button, which usually will activate a specific tool
public class ToolController : NetworkBehaviour, INetworkedPlayerModule {
    [SerializeField] private Transform _toolSlotMining; // Instantiated slot for the current mining tool
    [SerializeField] private Transform _toolSlotCleaning;

    // local only!
    private IToolBehaviour currentMiningToolBehavior; 
    private IToolBehaviour currentCleaningToolBehavior;
    private WorldManager _worldManager;

    // local for remote
    private Dictionary<ushort,IToolVisual> _idToToolVisual = new Dictionary<ushort, IToolVisual>();

    // Server
    private readonly SyncVar<ushort> _currentToolID = new SyncVar<ushort>();
    private readonly SyncVar<bool> _isUsingTool = new SyncVar<bool>(false);
    public SyncVar<ushort> CurrentToolID => _currentToolID;
    public SyncVar<bool> IsUsingTool => _isUsingTool;

    public int InitializationOrder => 9;

    public IToolVisual GetCurrentTool() {
        var id = CurrentToolID.Value;
        return _idToToolVisual.GetValueOrDefault(id);

    }
    public void InitializeOnOwner(NetworkedPlayer playerParent) {
        Console.RegisterCommand(this, "DEBUGSetMineTool", "setMineTool", "god");
        _worldManager = FindFirstObjectByType<WorldManager>();
        EquipDrill(); // Todo this would have to take from some kind of save file obviously
    }
    public override void OnStartServer() {
        base.OnStartServer();
        _worldManager = FindFirstObjectByType<WorldManager>();
        //if (App.isEditor) {
        //    DEBUGSetMineTool("drill");
        //    DEBUGSetDefaultCleanTool();
        //}
    }

    public void StopMining() {
        currentMiningToolBehavior?.ToolStop();
        StopUsingToolServerRpc(); // Let others know we've stopped 
    }
    public void StopCleaning() {
        currentCleaningToolBehavior?.ToolStop();
        StopUsingToolServerRpc(); // Let others know we've stopped 
    }

    /*
     * These two functions are called by the owning player, then, in PlayerVisualHandler attached to this playerObject on the 
     * remove client will recieve an OnChangeEvent saying "This player just started/stopped using their tool!" and then they
     * will locally simulate that tools visuals
    */
    [ServerRpc]
    private void StartUsingToolServerRpc() {
        _isUsingTool.Value = true;
    }
    [ServerRpc]
    private void StopUsingToolServerRpc() {
        _isUsingTool.Value = false;
    }

    public void PerformMining(InputManager input) {
        // Don't think we have to check this, because its called from the input manager, which will only be called by the owner
        if (!base.IsOwner)
            return;
        currentMiningToolBehavior?.ToolStart(input, this); // Delegate to tool behavior
        StartUsingToolServerRpc();
    }
    public void PerformCleaning(InputManager input) {
        if (!base.IsOwner)
            return;
        currentCleaningToolBehavior?.ToolStart(input, this);
        StartUsingToolServerRpc();
    }

    [ServerRpc(RequireOwnership = true)]
    public void CmdRequestDamageTile(Vector3 worldPos, short damageAmount) {
        // TODO: Server-side validation (range, tool, cooldowns, etc.)
        //Debug.Log($"worldPos {worldPos}, damageAmount {damageAmount} _Worldmanager: {_worldManager}");
        // Pass request to WorldGenerator for processing
        // TODO somehow _worldmanager is null here and it cant find it 
        if (_worldManager == null)
            _worldManager = FindFirstObjectByType<WorldManager>();
        _worldManager.RequestDamageTile(worldPos, damageAmount);
    }
    private void EquipDrill() {
        EquipMiningToolFromPrefab(App.ResourceSystem.GetPrefab("MiningDrill"));
    }
    private void DEBUGSetMineTool(string tool) {
        if (tool == "god") {
            Debug.Log("Setting Mining tool as GOD");
            EquipMiningToolFromPrefab(App.ResourceSystem.GetPrefab("DEBUGGOD"));
        } else if (tool == "drill") {
            // todo
            Debug.Log("Setting Mining tool as drill");
            EquipMiningToolFromPrefab(App.ResourceSystem.GetPrefab("MiningDrill"));
        } else if (tool == "laser") { 
            Debug.Log("Setting Mining tool as laser");
            EquipMiningToolFromPrefab(App.ResourceSystem.GetPrefab("MiningLazer"));
        }
    }
    private void DEBUGSetDefaultCleanTool() {
        EquipCleaningToolFromPrefab(App.ResourceSystem.GetPrefab("CleaningTool"));
        // var g = Instantiate(App.ResourceSystem.GetPrefab("CleaningTool"), transform);
        // base.Spawn(g, Owner); // This will be how you'd do it in multiplayer
    }

    internal void StopCurrentTool() {
        currentCleaningToolBehavior?.ToolStop();
        currentMiningToolBehavior?.ToolStop();
    }

    public void EquipMiningToolFromPrefab(GameObject toolPrefab) {
        EquipTool(toolPrefab, _toolSlotMining, ref currentMiningToolBehavior);
    }
    public void EquipCleaningToolFromPrefab(GameObject toolPrefab) {
        EquipTool(toolPrefab, _toolSlotCleaning, ref currentCleaningToolBehavior);
    }
    private void EquipTool(GameObject toolPrefab, Transform slot, ref IToolBehaviour toolBehaviorReference) {
        // 1. Clear out any existing tool in the slot first.
        ClearSlot(slot, ref toolBehaviorReference);

        // 2. If the prefab is null, we are just unequipping the slot. Exit early.
        if (toolPrefab == null)
            return;

        // 3. Validate that the prefab has the required component.
        if (toolPrefab.GetComponent<IToolBehaviour>() == null) {
            Debug.LogError($"Tool prefab '{toolPrefab.name}' is missing a component that implements IToolBehaviour.", toolPrefab);
            return;
        }
        // 4. Instantiate the tool and parent it to the designated slot.
        GameObject toolInstance = Instantiate(toolPrefab, slot); // Should use fishnet spawning here aswell!

        // 5. Get the behavior component and assign it.
        toolBehaviorReference = toolInstance.GetComponent<IToolBehaviour>();
        _currentToolID.Value = toolBehaviorReference.toolID;
    }
  
    // Will be called by a remote client so they can see the tool
    private void EquipToolVisual(GameObject toolPrefab) {
        // remote doesn't use slots
        GameObject toolInstance = Instantiate(toolPrefab, transform);
        IToolBehaviour toolBehaviour = null;
        if(toolInstance.TryGetComponent<IToolBehaviour>(out var b)) {
            toolBehaviour = b;
        } else {
            Debug.LogError("Could not find Interface on instantiated tool gameobject!");
        }
        // Store the instance so we can reference to it later
        _idToToolVisual.TryAdd(toolBehaviour.toolID, toolBehaviour.toolVisual);
    }
    public void EquipAllToolsVisualOnly() {
        EquipToolVisual(App.ResourceSystem.GetPrefab("MiningDrill"));
        EquipToolVisual(App.ResourceSystem.GetPrefab("MiningLazer"));
        //EquipToolVisual(App.ResourceSystem.GetPrefab("CleaningTool"));
    }
    public void ClearMiningSlot() {
        ClearSlot(_toolSlotMining, ref currentMiningToolBehavior);
    }

    public void ClearCleaningSlot() {
        ClearSlot(_toolSlotCleaning, ref currentCleaningToolBehavior);
    }


    private void ClearSlot(Transform slot, ref IToolBehaviour toolBehaviorReference) {

        // Destroy all GameObjects that are children of the slot
        foreach (Transform child in slot) {
            Destroy(child.gameObject);
        }

        // Clear the behavior reference
        toolBehaviorReference = null;
    }

    internal bool UnlockTool(string unlockName) {
        var toolPrefab = App.ResourceSystem.GetPrefab(unlockName);
        if (toolPrefab == null) {
            Debug.LogError($"Tool prefab '{unlockName}' not found in ResourceSystem.");
            return false;
        }
        // Check if the tool prefab has the required component
        if (toolPrefab.GetComponent<IToolBehaviour>() == null) {
            Debug.LogError($"Tool prefab '{unlockName}' is missing a component that implements IToolBehaviour.", toolPrefab);
            return false;
        }
        // Else set the tool 
        EquipMiningToolFromPrefab(toolPrefab);
        return true;
    }
}