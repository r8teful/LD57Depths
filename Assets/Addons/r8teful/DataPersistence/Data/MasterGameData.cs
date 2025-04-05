[System.Serializable]
public class MasterGameData {
    public long LastUpdated;
    public GameStateData GameData;
    public bool StoryComplete;
    public bool NewPlayer;
    public bool IsDemo;
    
    public bool BothExtremeLvl1; 
    public bool BothExtremeLvl2; 
    // the values defined in this constructor will be the default values
    // the game starts with when there's no data to load
    public MasterGameData() {
        //coinsCollected = new SerializableDictionary<string, bool>();
        GameData = new GameStateData();
        StoryComplete = false;
        IsDemo = false;
        NewPlayer = true;
        BothExtremeLvl1 = false;
        BothExtremeLvl2 = false;
    }

    public int ExmapleFunc() {
        // Do things with the data we have
        return 0;
    }
}