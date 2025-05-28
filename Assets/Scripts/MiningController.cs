using FishNet.Connection;
using FishNet.Object;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.InputSystem;

public class MiningController : NetworkBehaviour {
    private InputManager inputManager;
    private IMiningBehaviour currentToolBehavior; // Current tool's mining behavior
    private WorldManager _worldManager;
    public override void OnStartClient() {
        base.OnStartClient();
        if (!IsOwner) {
            base.enabled = false;
            return;
        }
        Console.RegisterCommand(this, "DEBUGSetMineTool", "setMineTool","god");
        //Console.RegisterCommand(this, "mineGod", "setMineTool", "laser");
            
        inputManager = GetComponent<InputManager>();
        _worldManager = FindFirstObjectByType<WorldManager>();
        if (App.isEditor) {
            DEBUGSetMineTool("laser");
        }
        
    }
    public override void OnStartServer() {
        base.OnStartServer();
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

    [ServerRpc(RequireOwnership = true)]
    public void CmdRequestDamageTile(Vector3 worldPos, short damageAmount) {
        // TODO: Server-side validation (range, tool, cooldowns, etc.)
        Debug.Log($"worldPos {worldPos}, damageAmount {damageAmount} _Worldmanager: {_worldManager}");
        // Pass request to WorldGenerator for processing
        // TODO somehow _worldmanager is null here and it cant find it 
        if (_worldManager == null)
            _worldManager = FindFirstObjectByType<WorldManager>();
        _worldManager.RequestDamageTile(worldPos, damageAmount);
    }
    public void SetMineTool(IMiningBehaviour tool) {
        currentToolBehavior = tool;
    }
    private void OnEnable() {
       
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