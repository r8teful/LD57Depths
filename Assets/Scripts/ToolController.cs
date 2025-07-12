using FishNet.Object;
using System.Security.Cryptography;
using UnityEngine;

// Controls what happens when the player presses the left mouse button, which usually will activate a specific tool
public class ToolController : NetworkBehaviour {
    [SerializeField] private Transform _toolSlotMining; // Instantiated slot for the current mining tool
    [SerializeField] private Transform _toolSlotCleaning;
    private IToolBehaviour currentMiningToolBehavior;
    private IToolBehaviour currentCleaningToolBehavior;
    private WorldManager _worldManager;
    public override void OnStartClient() {
        base.OnStartClient();
        if (!IsOwner) {
            base.enabled = false;
            return;
        }
        Console.RegisterCommand(this, "DEBUGSetMineTool", "setMineTool", "god");
        //Console.RegisterCommand(this, "mineGod", "setMineTool", "laser");
        _worldManager = FindFirstObjectByType<WorldManager>();
      

    }
    public override void OnStartServer() {
        base.OnStartServer();
        _worldManager = FindFirstObjectByType<WorldManager>();
        if (App.isEditor) {
            DEBUGSetMineTool("laser");
            DEBUGSetDefaultCleanTool();
        }
    }

    public void StopMining() {
        currentMiningToolBehavior?.ToolStop();
    }
    public void StopCleaning() {
        currentCleaningToolBehavior?.ToolStop();
    }


    // Set the current tool's behavior (called when equipping a tool)
    public void SetToolBehavior(IToolBehaviour toolBehavior) {
        currentMiningToolBehavior = toolBehavior;
    }

    public void PerformMining(InputManager input) {
        currentMiningToolBehavior?.ToolStart(input, this); // Delegate to tool behavior
    }
    public void PerformCleaning(InputManager input) {
        currentCleaningToolBehavior?.ToolStart(input, this);
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
    

    public void SetCleanTool(IToolBehaviour tool) {
        currentCleaningToolBehavior = tool;
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
}