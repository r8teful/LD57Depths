using FishNet.Object;
using UnityEngine;
using UnityEngine.InputSystem;

public class MiningController : NetworkBehaviour {
    private InputManager inputManager;
    private IMiningBehaviour currentToolBehavior; // Current tool's mining behavior
    private WorldManager _worldManager;
    public override void OnStartClient() {
        if (!IsOwner)
            return;
        Console.RegisterCommand(this, "DEBUGSetMineTool", "setMineTool","god");
        //Console.RegisterCommand(this, "mineGod", "setMineTool", "laser");
        
        inputManager = GetComponent<InputManager>();
        _worldManager = FindFirstObjectByType<WorldManager>();
    }

    public void OnMine(InputAction.CallbackContext context) {
        if (context.performed) {
            PerformMining(inputManager);
        } else if (context.canceled) {
            StopMining();
        }
    }

    private void StopMining() {
        currentToolBehavior?.MineStop(this);
    }

    // Set the current tool's behavior (called when equipping a tool)
    public void SetToolBehavior(IMiningBehaviour toolBehavior) {
        currentToolBehavior = toolBehavior;
    }

    private void PerformMining(InputManager input) {
        currentToolBehavior?.MineStart(input, this); // Delegate to tool behavior
    }

    //[ServerRpc]
    public void CmdRequestDamageTile(Vector3 worldPos, short damageAmount) {
        // TODO: Server-side validation (range, tool, cooldowns, etc.)

        // Pass request to WorldGenerator for processing
        _worldManager.RequestDamageTile(worldPos, damageAmount);
    }
    public void SetMineTool(IMiningBehaviour tool) {
        currentToolBehavior = tool;
    }

    private void DEBUGSetMineTool(string tool) {
        if (tool == "god") {
            Debug.Log("Setting Mining tool as GOD");
            SetMineTool(FindFirstObjectByType<DEBUGGOD>());
        } else if (tool == "drill") {
            // todo
            Debug.Log("Setting Mining tool as drill");
            SetMineTool(FindFirstObjectByType<DEBUGGOD>());
        } else if (tool == "laser") { 
            Debug.Log("Setting Mining tool as laser");
            SetMineTool(Instantiate(App.ResourceSystem.GetPrefab("MiningLazer"),transform).GetComponent<MiningLazer>());
        }
    }
}