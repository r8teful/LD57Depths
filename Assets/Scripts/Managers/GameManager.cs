using FishNet.Object;

// Holds game data? idk
public class GameManager : NetworkBehaviour {
    private string _upgradeTreeName = "DefaultTree"; // Would depend on what the player chooses for tools etc
    public string GetUpgradeTreeName() => _upgradeTreeName;
    public static GameManager LocalInstance { get; private set; }
    private void Awake() {
        if (LocalInstance != null && LocalInstance != this) Destroy(gameObject);
        else LocalInstance = this;
    }
}