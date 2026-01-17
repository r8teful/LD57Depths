using UnityEngine;

public class PlayerRewardManager : MonoBehaviour, INetworkedPlayerModule {
    public int InitializationOrder => 99;

    public void InitializeOnOwner(NetworkedPlayer playerParent) {

    }

    internal void GenerateRewards(int level) {
        // Don't think we care about level.


    }
    private UpgradeRecipeSO GetTreeUpgrade(int costValue) {
        return null;
    }
}