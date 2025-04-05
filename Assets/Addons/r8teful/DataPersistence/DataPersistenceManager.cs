using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

public class DataPersistenceManager : PersistentSingleton<DataPersistenceManager> {
    [Header("Debugging")]
    [SerializeField] private bool disableDataPersistence = false;
    [SerializeField] private bool initializeDataIfNull = false;
    [SerializeField] private bool overrideSelectedProfileId = false;
    [SerializeField] private string testSelectedProfileId = "test";

    [Header("File Storage Config")]
    [SerializeField] private string fileName;
    [SerializeField] private bool useEncryption;

    [Header("Auto Saving Configuration")]
    [SerializeField] private float autoSaveTimeSeconds = 60f;

    private MasterGameData _gameData;
    private List<IDataPersistence> dataPersistenceObjects;
    private FileDataHandler dataHandler;

    private string selectedProfileId = "";

    private Coroutine autoSaveCoroutine;

    protected override void Awake() {
        base.Awake();
        dataHandler = new FileDataHandler(Application.persistentDataPath, fileName, useEncryption);

        InitializeSelectedProfileId();
    }

    private void OnEnable() {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        dataPersistenceObjects = FindAllDataPersistenceObjects();
        LoadGame();

        // start up the auto saving coroutine
        if (autoSaveCoroutine != null) {
            StopCoroutine(autoSaveCoroutine);
        }
        autoSaveCoroutine = StartCoroutine(AutoSave());
    }

    public void ChangeSelectedProfileId(string newProfileId) {
        // update the profile to use for saving and loading
        selectedProfileId = newProfileId;
        // load the game, which will use that profile, updating our game data accordingly
        LoadGame();
    }

    public void DeleteProfileData(string profileId) {
        // delete the data for this profile id
        dataHandler.Delete(profileId);
        // initialize the selected profile id
        InitializeSelectedProfileId();
        // reload the game so that our data matches the newly selected profile id
        LoadGame();
    }

    private void InitializeSelectedProfileId() {
        selectedProfileId = dataHandler.GetMostRecentlyUpdatedProfileId();
        if (overrideSelectedProfileId) {
            selectedProfileId = testSelectedProfileId;
            Debug.LogWarning("Overrode selected profile id with test id: " + testSelectedProfileId);
        }
    }

    public void NewGame() {
        _gameData = new MasterGameData();
        Debug.Log("New Game");
    }

    public void NewGameStateData() {
        Debug.Log("New Game State Data");
        _gameData.GameData = new GameStateData();
        SaveGame();
    }

    public void LoadGame() {
        // return right away if data persistence is disabled
        if (disableDataPersistence) {
            return;
        }

        // load any saved data from a file using the data handler
        _gameData = dataHandler.Load(selectedProfileId);

        // start a new game if the data is null and we're configured to initialize data for debugging purposes
        if (_gameData == null && initializeDataIfNull) {
            NewGame();
        }

        // if no data can be loaded, don't continue
        if (_gameData == null) {
            Debug.Log("No data was found. A New Game needs to be started before data can be loaded.");
            return;
        }

        // push the loaded data to all other scripts that need it
        foreach (IDataPersistence dataPersistenceObj in dataPersistenceObjects) {
            dataPersistenceObj.LoadData(_gameData);
        }
    }

    public void SaveGame() {
        // return right away if data persistence is disabled
        if (disableDataPersistence) {
            return;
        }
        // if we don't have any data to save, log a warning here
        if (_gameData == null) {
            Debug.LogWarning("No data was found. A New Game needs to be started before data can be saved.");
            return;
        }

        // pass the data to other scripts so they can update it
        foreach (IDataPersistence dataPersistenceObj in dataPersistenceObjects) {
            dataPersistenceObj.SaveData(_gameData);
        }

        // timestamp the data so we know when it was last saved
        _gameData.LastUpdated = System.DateTime.Now.ToBinary();

        // save that data to a file using the data handler
        dataHandler.Save(_gameData, selectedProfileId);
    }

    protected override void OnApplicationQuit() {
        SaveGame();
        base.OnApplicationQuit();
    }

    private List<IDataPersistence> FindAllDataPersistenceObjects() {
        // FindObjectsofType takes in an optional boolean to include inactive gameobjects
        IEnumerable<IDataPersistence> dataPersistenceObjects = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,FindObjectsSortMode.None)
            .OfType<IDataPersistence>();

        return new List<IDataPersistence>(dataPersistenceObjects);
    }

    public bool HasGameData() {
        return _gameData != null;
    }

    public Dictionary<string, MasterGameData> GetAllProfilesGameData() {
        return dataHandler.LoadAllProfiles();
    }

    private IEnumerator AutoSave() {
        while (true) {
            yield return new WaitForSeconds(autoSaveTimeSeconds);
            SaveGame();
            Debug.Log("Auto Saved Game");
        }
    }
}
