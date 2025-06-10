using FishNet;
using FishNet.Connection;
using FishNet.Object;
using UnityEditor.PackageManager.Requests;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

// Controls what happens when the player presses the left mouse button, which usually will activate a specific tool
public class ToolController : NetworkBehaviour {
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
    
    
    public void SetMineTool(IToolBehaviour tool) {
        currentMiningToolBehavior = tool;
    }
    public void SetCleanTool(IToolBehaviour tool) {
        currentCleaningToolBehavior = tool;
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
    private void DEBUGSetDefaultCleanTool() {
        var g = Instantiate(App.ResourceSystem.GetPrefab("CleaningTool"), transform);
        base.Spawn(g, Owner);
        SetCleanTool(g.GetComponent<CleaningTool>());
    }

    internal void StopCurrentTool() {
        currentCleaningToolBehavior?.ToolStop();
        currentMiningToolBehavior?.ToolStop();
    }
}