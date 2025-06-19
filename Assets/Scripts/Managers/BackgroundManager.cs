using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundManager : MonoBehaviour {
    public GameObject trenchBackgroundContainer;
    public GameObject parallaxObjectsContainer; 
    private Transform[] layers; // The four parallax layers
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
        layers = new Transform[parallaxCount];
        objectsPerLayer = new int[layers.Length];
        foreach(var t in trenchBackgroundContainer.GetComponentsInChildren<TranchBackgroundSprite>()) {
            t.SetTrenchSettings(worldGenSettings);
            _trenchSprites.Add(t);
        }
       
        for (int i = 0; i < parallaxCount; i++)
        {
            layers[i] = parallaxObjectsContainer.transform.GetChild(i).transform;
        }

        // Centre
        trenchBackgroundContainer.transform.position = new Vector3(Camera.main.transform.position.x, Camera.main.transform.position.y, 0);
        trenchBackgroundContainer.transform.SetParent(Camera.main.transform);
        _blackSprite.enabled = false;
        biomeManager = bio;
       
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
        StartCoroutine(SpawnCoroutine());
        StartCoroutine(DespawnCoroutine());
    }
    private IEnumerator SpawnCoroutine() {
        // Wait until player is not null
        while (PlayerMovement.LocalInstance  == null) {
            yield return null; // Wait for the next frame
        }
        player = PlayerMovement.LocalInstance.transform;

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
                        Debug.Log("Spawned entitty");
                        GameObject newObj = Instantiate(data.prefab, new Vector3(spawnPos.x, spawnPos.y, 0), Quaternion.identity);
                        if (newObj.TryGetComponent<IBackgroundObject>(out var iBackground)) {
                            // iComp is your interface reference
                            iBackground.Init(_trenchSprites[layerIndex].BackgroundColor,layerIndex, _trenchSprites[layerIndex].OrderInLayer);
                        }
                        newObj.transform.SetParent(layers[layerIndex], true);
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
                }
            }
            // Remove and destroy objects outside despawn radius
            foreach (var obj in toRemove) {
                int layerIndex = System.Array.IndexOf(layers, obj.transform.parent);
                if (layerIndex >= 0) {
                    objectsPerLayer[layerIndex]--;
                }
                Destroy(obj,4f);
                if (obj.TryGetComponent<IBackgroundObject>(out var iBackground)) {
                    // iComp is your interface reference
                    iBackground.BeforeDestroy();
                }
                spawnedObjects[data].Remove(obj);
            }
        }
    }

    private int SelectLayerIndex() {
        float[] weights = new float[layers.Length];
        float totalWeight = 0f;
        for (int i = 0; i < layers.Length; i++) {
            weights[i] = 1f / (objectsPerLayer[i] + 1f);
            totalWeight += weights[i];
        }
        float rand = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        for (int i = 0; i < layers.Length; i++) {
            cumulative += weights[i];
            if (rand < cumulative) {
                return i;
            }
        }
        return layers.Length - 1; // Fallback
    }
}