using r8teful;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-100)] // Handles starting of games, saving of games, continueing of games etc
public class GameManager : PersistentSingleton<GameManager> {
    public WorldGenData WorldGenSettings => _currentGameSettings?.WorldGenSettings;
    private GameSettings _currentGameSettings;
    public GameSettings CurrentGameSettings => _currentGameSettings;
    [SerializeField] private PlayerManager _playerPrefab;
    private Coroutine _bootRoutine;
    // TODO remove this
    private string _upgradeTreeName = "DefaultTree"; // Would depend on what the player chooses for tools etc

    public static event Action OnSetupComplete;
    public bool IsBooting => _bootRoutine != null;
    public string GetUpgradeTreeName() => _upgradeTreeName;

    public bool DebugPlayDemo;
    private void OnEnable() {
        Debug.Log("GameSetupManager Enable!!");
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void Begin(GameSettings settings) {
        _currentGameSettings = settings;
        if(SceneManager.GetActiveScene().buildIndex == 0) {
            // From main menu 
            AudioController.Instance.SetLoopVolume(0, 4); // Stop main menu music
        }
        _bootRoutine = StartCoroutine(BootSequence());
        //StartCoroutine(App.Backdrop.Require());
        //SceneManager.LoadScene(1);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        if (scene.name == ResourceSystem.ScenePlayName) {
            if (_bootRoutine != null) 
                return; // already running just return 
            if (_currentGameSettings == null) {
                // We've run this scene from the editor, or something went very wrong. Just create one here
                _currentGameSettings = new GameSettings(createRandomSeed: true);
            }
            _bootRoutine = StartCoroutine(BootSequence(loadPlayScene: false));
        }
    }
    private IEnumerator BootSequence(bool loadPlayScene = true) {
        yield return App.Backdrop.Require();
        if (loadPlayScene) { // we only have to load if if we are calling from main menu 
            SceneManager.LoadScene(1); // here you'd do async or something if you're showing the lore
        }

        Debug.Log($"boot seq start: {GetInstanceID()}");
        SaveData saveData = _currentGameSettings.SaveToLoad;
        
        //SetupSettings(true,s);
        
        yield return null;

        // Apparently this is fine? They're singletons anyway and we're on a loading screen so I feel like no one will notice
        WorldManager w = FindFirstObjectByType<WorldManager>();
        if (w == null) 
            LogError(w);
        
        w.Init(this);
        
        yield return null;
        
        // This just does biomes. Not caves or anything 
        var bm = FindFirstObjectByType<BiomeMaterialUploader>();
        if (bm == null)
            LogError(bm);
        bm.PushBiomesToMaterial(WorldGenSettings);
        yield return null;
        
        var bw = FindFirstObjectByType<BackgroundWorldTexturesHandler>();
        if (bw == null)
            LogError(bw);
        bw.PushBiomesToMaterials(WorldGenSettings);

        // Spawn structures 
        var str = FindFirstObjectByType<StructureManager>();
        if (str == null)
            LogError(str);
        if(saveData != null) {
            if(!saveData.HasRunData) 
                str.SpawnStructures(); // only spawn new structures if we're actually making a new world
        } else {
            str.SpawnStructures(); 

        }
            var e = FindFirstObjectByType<EntityManager>();
        if (e == null)
            LogError(e);
        if (saveData != null) {
            Debug.Log($"Loading entities...");
            e.OnLoad(saveData);
        }

        Debug.Log($"Spawning Player");
        // Spawn player
        var p = Instantiate(_playerPrefab, w.PlayerSpawn,Quaternion.identity);
        p.Init(saveData); // Player handles setup of player modules and loading of saveData.BobData
        
        yield return null;
        // Wait for chunk manager to generate the world
        var chunkM = FindFirstObjectByType<ChunkManager>();
        if (chunkM == null)
            LogError(chunkM);
        if (saveData != null) {
            Debug.Log($"Loading chunkData...");
            chunkM.OnLoad(saveData);
        }
        chunkM.Init(w);

        yield return new WaitUntil(() => chunkM.HasStartedLoadingRoutine);
        Debug.Log($"Generating Chunks...");
        //yield return new WaitUntil(()=> !chunkM.IsGenerating(),new TimeSpan(0,2,0),()=> Debug.LogError("Took too long to generate!"));
        yield return null;
        yield return new WaitWhile(()=> chunkM.IsGenerating);
        Debug.Log("finished generating chunks");

        var biomeM = FindFirstObjectByType<BiomeManager>();
        biomeM.Init(w);
        yield return null;
        
        var sub = FindFirstObjectByType<SubmarineManager>();
        if (sub == null)
            LogError(sub);
        if (saveData != null) {
            sub.OnLoad(saveData);
        }


        p.PlayerLayerController.PutPlayerInSub();// Is it that easy? lol


        AudioController.Instance.SetLoopAndPlay("Ambience", 1);
        yield return null;// App.Backdrop.Release();
        
        _bootRoutine = null;
        yield return App.Backdrop.Release();
        Debug.Log("Boot sequence complete!");
        OnSetupComplete?.Invoke();
    }

    private void Save() {
        SaveData saveData = new SaveData();

        // Trigger save for monobehaviours
        if (PlayerManager.Instance != null) {
            PlayerManager.Instance.UpgradeManager.OnSave(saveData);

            if (ChunkManager.Instance != null) {
                ChunkManager.Instance.OnSave(saveData);
            } else {
                Debug.LogWarning("Chunk manager not found!");
            }
            if (SubmarineManager.Instance != null) {
                SubmarineManager.Instance.OnSave(saveData);
            } else {
                Debug.LogWarning("SubmarineManager manager not found!");
            }
            if (EntityManager.Instance != null) {
                EntityManager.Instance.OnSave(saveData);
            } else {
                Debug.LogWarning("EntityManager manager not found!");
            }
        } else {
            Debug.LogWarning("Player not found, will not save run state");
        }
        // We write seed
        saveData.worldData.Seed = WorldGenSettings.seed;

        // Save manager will write meta files and save into memory
        SaveManager.Save(saveData);
    }
    public void TriggerSave() {
        Save();
    }

    public void Load() {

    }
    private void LogError(object script) {
        Debug.LogError($"Coudn't find script of type {script.GetType()}!!");
    }


    public void OnDrawGizmos() {
        if (WorldGenSettings == null)
            return;
        foreach (var ore in WorldGenSettings.worldOres) {
            //Vector2 center = new Vector2(0, _worldGenSettings.MaxDepth);
            Vector2 center = ore.oreStart;
            var color = ore.DebugColor;
            var targetR = ore.WorldDepthBandProcent * Mathf.Abs(WorldGenSettings.MaxDepth);
            float bandWidth = targetR * ore.widthPercent; 
            DrawWireCircle(center, targetR, color);

            // How to include bandwidth so that we show these circles when the falloff is practicly 0??
            float tLimit = Mathf.Sqrt(-Mathf.Log(0.1f));
            float delta = bandWidth * tLimit;
            float outer = targetR + delta;
            float inner = Mathf.Max(0f, targetR - delta); // clamp so radius doesn't go negative

            Color faded = color; faded.a = 0.15f;
            DrawWireCircle(center, outer, faded);
            DrawWireCircle(center, inner, faded);
        }
    }

    private void DrawWireCircle(Vector2 center, float radius, Color color) {
        var segments = 64;
        float angleStep = 2f * Mathf.PI / segments;
        Vector3 prevPoint = center + Vector2.right * radius;

        for (int i = 1; i <= segments; i++) {
            float angle = angleStep * i;
            Vector3 nextPoint = center + new Vector2(
                Mathf.Cos(angle),
                Mathf.Sin(angle)
            ) * radius;
            Gizmos.color = color;
            Gizmos.DrawLine(prevPoint, nextPoint);
            prevPoint = nextPoint;
        }
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
  