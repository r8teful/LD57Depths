using UnityEngine;
// Local instance of a tool visual
public interface IToolVisual {
    /// <summary>
    /// Should handle a single frame worths of visuals
    /// </summary>
    /// <param name="inputManager">current inputManager</param>
    /// 

    /* Start, Stop, and Update Visuals are handled differently depending if we are the owning client or not
     * When we are the owning client, they will be properly called by the MiningBase and CleaningTool scripts
     * When we are NOT the owning client, they will be called through the OnChange event with the networked variables in the
     * ToolController, this controller will also then specify when, what, and where the tool is used for that specific client
     * With this information, we can then succesfully simulate the visual of the tool locally on the client without it actually
     * effecting any gameplay, but still visually syncing with what the other player is doing.
     */
    public void HandleVisualUpdate(Vector2 nextInput, InputManager input, bool isAbility); // Can update visuals using two ways, either directly using nextInput, or other ways using inputManager
    public void HandleVisualUpdateRemote(Vector2 nextInput);
    public void HandleVisualStart(PlayerVisualHandler playerVisualHandler);
    public void HandleVisualStop(PlayerVisualHandler playerVisualHandler);

    // Should stash the IToolBehaviour so we can call GetToolData for visuals, because they change and depend on the tool
    public void Init(IToolBehaviour parent);
}