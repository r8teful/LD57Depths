using r8teful;
using System;
using UnityEngine;

public class PlayerRewardManager : MonoBehaviour, INetworkedPlayerModule {
    public int InitializationOrder => 99;
    private IExecutable[] _upgradeEffects = new IExecutable[3];
    private NetworkedPlayer _player;

    public IExecutable[] UpgradeEffects => _upgradeEffects;
    public void InitializeOnOwner(NetworkedPlayer playerParent) {
        _upgradeEffects = new IExecutable[3];
        _player = playerParent;
    }

    internal void GenerateRewards(int level) {
        int rewardsMade = 0;
        int safetyTries = 0;
        while(rewardsMade < 3 || safetyTries > 10000) {
            if (TryCreateReward(rewardsMade)) {
                rewardsMade++;
            }
            safetyTries++;
        }
    }
    private bool TryCreateReward(int rewardNumber) {
        int[] weights = { 1, 4, 5 };
        int i = RandomnessHelpers.PickIndex(weights);

        // Very cool
        var pickers = new Func<IExecutable>[] {
            () => TryGetAbilityReward(),
            () => TryGetAbilityUpgradeReward(),
            () => TryGetTreeUpgradeReward(30, out _)
        };

        // safety
        if (i < 0 || i >= pickers.Length) return false;
        _upgradeEffects[rewardNumber] = pickers[i]();
        return pickers[i]() != null;
    }

    private IExecutable TryGetAbilityUpgradeReward() {
        // Check if we have a valid ability to upgrade.
        // Roll rarity etc...
        return null;
    }

    private IExecutable TryGetAbilityReward() {
        // Check if we have enough ability slots, etc..
        var ex = _player.PlayerAbilities.OwnedAbilities;
        var a = App.ResourceSystem.GetRandomAvailableAbility(ex);
        return new AddAbilityEffect(a); // I'm a genius 
    }

    private IExecutable TryGetTreeUpgradeReward(int costValue, out UpgradeRecipeSO upgrade) {
        var tree = App.ResourceSystem.GetTreeByName(GameSetupManager.LocalInstance.GetUpgradeTreeName());
        upgrade = tree.GetUpgradeWithValue(costValue);
        return upgrade;
    }

}