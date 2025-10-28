// Any Behaviour that is on the player should implement this so we can properly handle it in NetworkedPlayer.cs
public interface INetworkedPlayerModule {
    /// <summary>
    /// The initialization order for this module. Lower numbers execute first.
    /// </summary>
    int InitializationOrder { get; }
    /* We want to start by initializing all the component that don't do any logic, because that could cause null reference exceptions
    these for now are: Crafting, PlayerVisual, PlayerCameraController

    Note:
    PlayerCameraController needs WorldVisibilityManager
    
    These script do have logic:
    UpgradeManager: needs CraftingComponent when initialized 
    PlayerLayerController: Calls HandleClientContextChange() on Init which relies on some scripts to be setup
    UIManager: Relies on Upgrade, Inventory, 
    InputManager: Relies on UI & ToolController
    PlayerMovement: Sets state when initialize which calls PlayerVisualHandler
    Tool controller needs PlayerStatsManager as tools init depending on playerStats
    */
    /// <summary>
    /// Called by the NetworkedPlayerSetup orchestrator to initialize the module.
    /// </summary>
    /// <param name="playerParent">A reference to the main setup script, which can be used to access other modules or shared data.</param>
    void InitializeOnOwner(NetworkedPlayer playerParent);
}