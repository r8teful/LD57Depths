// Any Behaviour that is on the player should implement this so we can properly handle it in NetworkedPlayer.cs
public interface INetworkedPlayerModule {
    /// <summary>
    /// The initialization order for this module. Lower numbers execute first.
    /// </summary>
    int InitializationOrder { get; }

    /// <summary>
    /// Called by the NetworkedPlayerSetup orchestrator to initialize the module.
    /// </summary>
    /// <param name="playerParent">A reference to the main setup script, which can be used to access other modules or shared data.</param>
    void InitializeOnOwner(NetworkedPlayer playerParent);
}