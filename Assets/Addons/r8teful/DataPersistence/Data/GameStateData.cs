[System.Serializable]
public class GameStateData {

    
    public int DeathCountLvl1;
    public int DeathCountLvl2;
    // 0: Nothing complete
    // 1: Intro complete
    // 2: Level 1 complete
    // 3: Intermission Complete
    // 4: Level 2 complete
    // 5: Ending complete 
    public int Progression; // Used for loading into player into game
    public bool lvl1TutorialComplete; 
    public bool lvl1MiddleComplete; // Has the player heard the "I've got another challenge for you" lines? 
    public bool controlRoomSeen; // The little intro at the start
    public bool lvl2TutorialComplete; // The little intro at the start

    public GameStateData() {
        DeathCountLvl1 = 0;
        DeathCountLvl2 = 0;
        Progression = 0;
        lvl1TutorialComplete = false;
        lvl1MiddleComplete = false;
        controlRoomSeen = false;
        lvl2TutorialComplete = false;
    }
}
