using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundManager : MonoBehaviour {
    public GameObject trenchBackgroundContainer;
    public GameObject parallaxObjectsContainer;
    public ParticleSystem trashParticles;
    private Transform[] _parallaxLayers; // The four parallax layers
    public SpriteRenderer _blackSprite;
    private List<TranchBackgroundSprite> _trenchSprites= new List<TranchBackgroundSprite>();

    private List<BackgroundObjectSO> backgroundObjectDatas;
    private Transform player;
    [SerializeField] private float spawnRadius = 10f;
    [SerializeField] private float despawnRadius = 15f;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float despawnInterval = 1f;


    private Dictionary<BackgroundObjectSO, List<GameObject>> spawnedObjects = new Dictionary<BackgroundObjectSO, List<GameObject>>();
    private BiomeManager biomeManager;
    private int[] objectsPerLayer;
    internal void Init(WorldGenSettingSO worldGenSettings, BiomeManager bio) {
        var parallaxCount = parallaxObjectsContainer.transform.childCount;
        _parallaxLayers = new Transform[parallaxCount];
        objectsPerLayer = new int[_parallaxLayers.Length];
        foreach(var t in trenchBackgroundContainer.GetComponentsInChildren<TranchBackgroundSprite>()) {
            t.SetTrenchSettings(worldGenSettings);
            _trenchSprites.Add(t);
        }
        
        for (int i = 0; i < parallaxCount; i++)
        {
            _parallaxLayers[i] = parallaxObjectsContainer.transform.GetChild(i).transform;
        }

        // Centre
        trenchBackgroundContainer.transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, 0);
        trenchBackgroundContainer.transform.SetParent(Camera.main.transform);
        _blackSprite.enabled = false;
        biomeManager = bio;

        bio.OnNewClientBiome += NewClientBiome;
        // Spawn particle systems
        //SpawnTrashParticles();
        
    }

    private void NewClientBiome(BiomeType biomeOld, BiomeType biomeNew) {
        // This will only work for server host but you should make it so that its run locally on the client
        // This now means that we should locally change particles / lighting / etc.

    }

    private void SpawnTrashParticles() {
        var parMain = Instantiate(trashParticles, Camera.main.transform).main;
        parMain.simulationSpace = ParticleSystemSimulationSpace.World;


        var parMain2 = Instantiate(trashParticles, Camera.main.transform).main;
        parMain2.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.08f);
        parMain2.simulationSpace = ParticleSystemSimulationSpace.Custom;
        parMain2.customSimulationSpace = _parallaxLayers[1];
        parMain2.maxParticles = 200;

        var parMain3 = Instantiate(trashParticles, Camera.main.transform).main;
        parMain3.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.05f);
        parMain3.simulationSpace = ParticleSystemSimulationSpace.Custom;
        parMain3.customSimulationSpace = _parallaxLayers[3];
        parMain3.maxParticles = 200;
    }

    public void SetInteriorBackground(bool isInterior) {
        if (isInterior) {
            _blackSprite.enabled = true;
        } else {
            _blackSprite.enabled = false;
        }
    }

    void Start() {
        // Initialize the dictionary with empty lists for each background object data
        backgroundObjectDatas = App.ResourceSystem.BackgroundObjects;
        foreach (var data in backgroundObjectDatas) {
            spawnedObjects[data] = new List<GameObject>();
        }
        // Do this later
        //StartCoroutine(SpawnCoroutine());
        //StartCoroutine(DespawnCoroutine());
    }
    private IEnumerator SpawnCoroutine() {
        // Wait until player is not null
        while (NetworkedPlayer.LocalInstance  == null) {
            yield return null; // Wait for the next frame
        }
        player = NetworkedPlayer.LocalInstance.transform;

        // Continue spawning at intervals
        while (true) {
            SpawnObjects();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private IEnumerator DespawnCoroutine() {
        // Wait until player is not null
        while (player == null) {
            yield return null; // Wait for the next frame
        }

        // Continue despawning at intervals
        while (true) {
            DespawnObjects();
            yield return new WaitForSeconds(despawnInterval);
        }
    }

    void SpawnObjects() {
        Vector2 playerPos = new Vector2(player.position.x, player.position.y);
        foreach (var data in backgroundObjectDatas) {
            // Check biome compatibility (if biome is specified)
            //Debug.Log(biomeManager.GetCurrentClientBiome());
            if (data.biomes.Contains(biomeManager.GetCurrentClientBiome())) {
                int currentCount = spawnedObjects[data].Count;
                // Check if we can spawn more instances
                if (currentCount < data.maxInstances) {
                    // Roll for spawn chance
                    if (Random.value < data.spawnLikelihood) {
                        // Select a layer based on current layer counts
                        int layerIndex = SelectLayerIndex();
                        // Generate a random position within spawn radius
                        Vector2 spawnPos = playerPos + Random.insideUnitCircle * spawnRadius;
                        //Debug.Log("Spawned entitty");
                        GameObject newObj = Instantiate(data.prefab, new Vector3(spawnPos.x, spawnPos.y, 0), Quaternion.identity);
                        if (newObj.TryGetComponent<IBackgroundObject>(out var iBackground)) {
                            // iComp is your interface reference
                            iBackground.Init(_trenchSprites[layerIndex].BackgroundColor,layerIndex, _trenchSprites[layerIndex].OrderInLayer);
                        }
                        newObj.transform.SetParent(_parallaxLayers[layerIndex], true);
                        spawnedObjects[data].Add(newObj);
                        objectsPerLayer[layerIndex]++;
                    }
                }
            }
        }
    }

    void DespawnObjects() {
        Vector2 playerPos = new Vector2(player.position.x, player.position.y);
        foreach (var data in backgroundObjectDatas) {
            List<GameObject> toRemove = new List<GameObject>();
            foreach (var obj in spawnedObjects[data]) {
                Vector2 objPos = new Vector2(obj.transform.position.x, obj.transform.position.y);
                // Check if object is outside despawn radius
                if (Vector2.Distance(objPos, playerPos) > despawnRadius) {
                    toRemove.Add(obj);
                    //Debug.Log($"Despawned object: {obj.name} at position {objPos} outside radius {despawnRadius}");
                }
            }
            // Remove and destroy objects outside despawn radius
            foreach (var obj in toRemove) {
                int layerIndex = System.Array.IndexOf(_parallaxLayers, obj.transform.parent);
                if (layerIndex >= 0) {
                    objectsPerLayer[layerIndex]--;
                }
                if (obj.TryGetComponent<IBackgroundObject>(out var iBackground)) {
                    // iComp is your interface reference
                    iBackground.BeforeDestroy();
                }
                spawnedObjects[data].Remove(obj);
            }
        }
    }

    private int SelectLayerIndex() {
        float[] weights = new float[_parallaxLayers.Length];
        float totalWeight = 0f;
        for (int i = 0; i < _parallaxLayers.Length; i++) {
            weights[i] = 1f / (objectsPerLayer[i] + 1f);
            totalWeight += weights[i];
        }
        float rand = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < _parallaxLayers.Length; i++) {
            cumulative += weights[i];
            if (rand < cumulative) {
                return i;
            }
        }
        return _parallaxLayers.Length - 1; // Fallback
    }
}