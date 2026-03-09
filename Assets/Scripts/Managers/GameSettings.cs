using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;



[Serializable]
public class GameSettings {
    public GameSettings() {

    }

    public GameSettings(bool createRandomSeed) : this(createRandomSeed ? NewSeed() : 0) {
    }
    public GameSettings(int seed) : this(ResourceSystem.GetMainMap(),seed) {
    }

    public GameSettings(WorldGenSettingSO template, int seed) {
        GameStartMode = StartMode.NewGame;
        UnityEngine.Random.InitState(seed); // Important to get save results for each seed
        WorldGenSettings = WorldGenData.FromSO(template, true, seed);
        SaveToLoad = null;
    }

    public StartMode GameStartMode;

    //public DifficultyConfig Difficulty;

    public WorldGenData WorldGenSettings;

    //public ChallengeDefinition Challenge;       // predefined, not player-authored

    public WorldSaveData SaveToLoad; 
    
    public ushort[] EnabledModifierIds = Array.Empty<ushort>();

    // Wil have to clean this up later 
    public List<AbilitySO> AvailableAbilities;
    public List<EventCaveSO> AvailableEventcaves; // Either take all from resource system or just setting defined idk

    public HashSet<ushort> AvailableAbilityIDs 
        => AvailableAbilities.Select(a => a.ID).ToHashSet();
    public HashSet<ushort> AvailableEventCaveIDs
        => AvailableEventcaves.Select(a => a.ID).ToHashSet();

    
    public enum StartMode {
        NewGame,
        Continue,
        Challenge
    }
    private static int NewSeed() {
        byte[] bytes = new byte[4];
        using (var rng = RandomNumberGenerator.Create()) {
            rng.GetBytes(bytes);
        }
        int seed = BitConverter.ToInt32(bytes, 0);
        // make non-negative
        return seed & 0x7FFFFFFF;
    }
}