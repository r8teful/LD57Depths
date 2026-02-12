using r8teful;
using Sirenix.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerRewardManager : MonoBehaviour, IPlayerModule {
    public int InitializationOrder => 99;
    private IExecutable[] _rewardEffects = new IExecutable[3];
    private PlayerManager _player;

    // These hashsets makes sure we don't pick the same reward multiple times
    private HashSet<ushort> pickedAbilityIDs =         new();
    private HashSet<ushort> pickedAbilityUpgradeIDs =  new();
    private HashSet<ushort> pickedUpgradeNodeIDs =     new();
    private HashSet<StatType> pickedStats =            new();
    public IExecutable[] UpgradeEffects => _rewardEffects;
    public void InitializeOnOwner(PlayerManager playerParent) {
        _rewardEffects = new IExecutable[3];
        _player = playerParent;
    }

    internal void GenerateRewardsLevel(int level) {
        int rewardsMade = 0;
        int safetyTries = 0;
        while(rewardsMade < 3 && safetyTries < 1000) {
            if (TryCreateLevelReward(rewardsMade)) {
                rewardsMade++;
            }
            safetyTries++;
        }
        // clear hashsets
        pickedAbilityIDs.Clear();
        pickedAbilityUpgradeIDs.Clear();
        pickedUpgradeNodeIDs.Clear();
        pickedStats.Clear();
    }

    public void GenerateRewardsChest() {
        int rewardsMade = 0;
        int safetyTries = 0;
        while (rewardsMade < 3 && safetyTries < 1000) {
            if (TryCreateChestReward(rewardsMade)) {
                rewardsMade++;
            }
            safetyTries++;
        }
    }

    public void GenerateRewardsShrine() {
        int rewardsMade = 0;
        int safetyTries = 0;
        while (rewardsMade < 3 && safetyTries < 1000) {
            if (TryCreateShrineReward(rewardsMade)) {
                rewardsMade++;
            }
            safetyTries++;
        }
    }
    internal void GenerateRewardCave() {
        int rewardsMade = 0;
        int safetyTries = 0;
        while (rewardsMade < 1 && safetyTries < 1000) {
            if (TryCreateCaveReward(rewardsMade)) {
                rewardsMade++;
            }
            safetyTries++;
        }
    }

   

    private bool TryCreateShrineReward(int rewardsMade) {
        // Todo make some kind of function that calculates some reasonable resources 
        List<StatModifier> stats = ResourceSystem.GetStatRewards();
        var randomStat = stats.OrderBy(_ => UnityEngine.Random.value).First();
        // Todo can't be same stat as already chosen
        if (pickedStats.Contains(randomStat.Stat)) return false;
        int[] weights = ResourceSystem.GetRarityWeight;
        var i = RandomnessHelpers.PickIndexWithLuck(weights, _player.PlayerStats.GetStat(StatType.Luck));
        var modValue = randomStat.Value * ResourceSystem.GetIncreaseByRarity((RarityType)i);
        var reward = new ShrineRewardEffect(new(modValue, randomStat.Stat,randomStat.Type,null));
        _rewardEffects[rewardsMade] = reward;
        pickedStats.Add(randomStat.Stat);
        return true;
    }
    private bool TryCreateChestReward(int rewardsMade) {
        List<ItemQuantity> items = new List<ItemQuantity>();
        // Todo make some kind of function that calculates some reasonable resources 
        if (UpgradeManagerPlayer.Instance == null) return false;
        var nodes = UpgradeManagerPlayer.Instance.GetUpgradesForChests();
        items.AddRange(RandomnessHelpers.GetChestRewards(nodes));

        int XpToGain = 0;
        var reward = new ChestRewardEffect(items,XpToGain);
        _rewardEffects[rewardsMade] = reward;
        return true;
    }
    private bool TryCreateCaveReward(int rewardsMade) {
        

        var reward = new CaveRewardEffect();
        _rewardEffects[rewardsMade] = reward;
        return true;
    }

    private bool TryCreateLevelReward(int rewardNumber) {
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
        _rewardEffects[rewardNumber] = pickers[i]();
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
        int[] weights = ResourceSystem.GetRarityWeight;
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
        //pickedUpgradeNodeIDs.Add(upgrade.ID);
        return upgrade;
    }

    internal void ExecuteReward(IExecutable choice) {
        if(choice == null) return;
        if (choice is AddAbilityEffect a) {
            // no special logic here
        } else if (choice is UpgradeStage u) {
            // Need to manually add to upgradeManager which is annoying but kind of works
            // alternatively we could have something similar like the AddAbilityEffect script that just calls this line
            // in the execute method
            //_player.UpgradeManager.AddUnlockedUpgrade(u);
        } else {
            // no special logic here
        }
        choice.Execute(new(_player));
    }

}