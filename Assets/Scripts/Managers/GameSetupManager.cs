using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[DefaultExecutionOrder(-100)]
public class GameSetupManager : PersistentSingleton<GameSetupManager> {
    public GameSettings CurrentGameSettings;

    // TODO remove this
    private string _upgradeTreeName = "DefaultTree"; // Would depend on what the player chooses for tools etc
    public string GetUpgradeTreeName() => _upgradeTreeName;
    
    public void AddWorldGenSettings(WorldGenSettingSO settings) {
        CurrentGameSettings.WorldSeed = settings.seed;
        // todo add other data???
    }
   

}

[Serializable]
public class GameSettings {
    public int WorldSeed;
    public ushort WorldGenID;
    public ushort[] EnabledModifierIds = Array.Empty<ushort>();
    public List<AbilitySO> AvailableAbilities;

    public HashSet<ushort> AvailableAbilityIDs 
        => AvailableAbilities.Select(a => a.ID).ToHashSet();

    public GameSettings(int worldSeed, ushort[] enabledModifierIds) {
        WorldSeed = worldSeed;
        EnabledModifierIds = enabledModifierIds;
    }
    public GameSettings(WorldGenSettingSO worldGenData) {
        WorldSeed = worldGenData.seed;
        EnabledModifierIds = Array.Empty<ushort>();
    }

}

[Serializable]
public struct CharacterData {
    public ushort CharacterId;
    public ushort CosmeticId;
    public ushort[] StartingEquipmentIds;
}

public enum Difficulty {
    Easy = 0,
    Normal = 1,
    Hard = 2,
    Custom = 3
}
  