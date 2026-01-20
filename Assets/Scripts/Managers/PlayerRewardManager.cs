using r8teful;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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
            () => TryGetAbilityUpgradeReward(),
            () => TryGetAbilityReward(),
            () => TryGetTreeUpgradeReward(30, rewardNumber)
        };

        // safety
        if (i < 0 || i >= pickers.Length) return false;
        _upgradeEffects[rewardNumber] = pickers[i]();
        return pickers[i]() != null;
    }

    private IExecutable TryGetAbilityUpgradeReward() {
        // Pick ability to upgrade
        if (!_player.PlayerAbilities.TryGetUpgradeableAbilities(out var abilities))
            return null;
        var rnd = new System.Random();
        var a = abilities[rnd.Next(abilities.Count)];

        // Pick a value to upgrade
        var u = a.Data.UpgradeValues[rnd.Next(a.Data.UpgradeValues.Count)];

        // Pick rarity
        int[] weights = { 70, 15, 6, 2 };
        var i = RandomnessHelpers.PickIndexWithLuck(weights, _player.PlayerStats.GetStat(StatType.Luck));
        var ModValue = u.Value * ResourceSystem.GetIncreaseByRarity((RarityType)i);
        var mod = new StatModifier(ModValue, u.Stat, u.Type, this);
        return new AbilityUpgradeEffect(a, mod, (RarityType)i);
    }

    private IExecutable TryGetAbilityReward() {
        // Check if we have enough ability slots, etc..
        var ex = _player.PlayerAbilities.OwnedAbilitiesIDs;
        ex.AddRange(pickedAbilityIDs);
        var a = App.ResourceSystem.GetRandomAvailableAbility(ex);
        if(a == null) return null;
        pickedAbilityIDs.Add(a.ID);
        return new AddAbilityEffect(a); // I'm a genius 
    }

    private IExecutable TryGetTreeUpgradeReward(int costValue,int rewardNumber) {
        var tree = App.ResourceSystem.GetTreeByName(GameSetupManager.Instance.GetUpgradeTreeName());
        var upgrade = tree.GetUpgradeWithValue(costValue, pickedUpgradeNodeIDs);
        if(upgrade == null) return null;
        pickedUpgradeNodeIDs.Add(upgrade.ID);
        return upgrade;
    }

    internal void ExecuteReward(IExecutable choice) {
        if(choice == null) return;
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