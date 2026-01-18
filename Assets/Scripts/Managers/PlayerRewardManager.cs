using r8teful;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.InputSystem.LowLevel.InputStateHistory;

public class PlayerRewardManager : MonoBehaviour, INetworkedPlayerModule {
    public int InitializationOrder => 99;
    private IExecutable[] _upgradeEffects = new IExecutable[3];
    private NetworkedPlayer _player;
    private HashSet<ushort> pickedAbilityIDs =         new();
    private HashSet<ushort> pickedAbilityUpgradeIDs =  new();
    private HashSet<ushort> pickedUpgradeNodeIDs =     new();
    public IExecutable[] UpgradeEffects => _upgradeEffects;
    public void InitializeOnOwner(NetworkedPlayer playerParent) {
        _upgradeEffects = new IExecutable[3];
        _player = playerParent;
    }

    internal void GenerateRewards(int level) {
        int rewardsMade = 0;
        int safetyTries = 0;
        while(rewardsMade < 3 && safetyTries < 10000) {
            if (TryCreateReward(rewardsMade)) {
                rewardsMade++;
            }
            safetyTries++;
        }
        // clear hashsets
        pickedAbilityIDs.Clear();
        pickedAbilityUpgradeIDs.Clear();
        pickedUpgradeNodeIDs.Clear();
    }
    private bool TryCreateReward(int rewardNumber) {
        int[] weights = { 1, 4, 5 };
        int i = RandomnessHelpers.PickIndex(weights);
        // Very cool
        var pickers = new Func<IExecutable>[] {
            () => TryGetAbilityReward(),
            () => TryGetAbilityUpgradeReward(),
            () => TryGetTreeUpgradeReward(30, rewardNumber)
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
        ex.AddRange(pickedAbilityIDs);
        var a = App.ResourceSystem.GetRandomAvailableAbility(ex);
        if(a == null) return null;
        pickedAbilityIDs.Add(a.ID);
        return new AddAbilityEffect(a); // I'm a genius 
    }

    private IExecutable TryGetTreeUpgradeReward(int costValue,int rewardNumber) {
        var tree = App.ResourceSystem.GetTreeByName(GameSetupManager.LocalInstance.GetUpgradeTreeName());
        var upgrade = tree.GetUpgradeWithValue(costValue, pickedUpgradeNodeIDs);
        if(upgrade == null) return null;
        pickedUpgradeNodeIDs.Add(upgrade.ID);
        return upgrade;
    }

    internal void ExecuteReward(IExecutable choice) {
        if (choice is AddAbilityEffect a) {
            // no special logic here
        } else if (choice is UpgradeRecipeSO u) {
            // Need to manually add to upgradeManager which is annoying but kind of works
            // alternatively we could have something similar like the AddAbilityEffect script that just calls this line
            // in the execute method
            _player.UpgradeManager.AddUnlockedUpgrade(u.ID);
        } else {
            // no special logic here
        }
        choice.Execute(new(_player));
    }
}