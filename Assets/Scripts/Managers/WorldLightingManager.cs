using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using Random = UnityEngine.Random;
using FishNet.Object;
using System.Collections;
using DG.Tweening;

[System.Serializable]
public struct LightProperties {
    public Color color;
    public float intensity;
    // Potentially falloff, etc.
}
public class WorldLightingManager : NetworkBehaviour {
    [SerializeField] private ChunkManager _chunkManager;
    [SerializeField] private WorldManager _worldManager;
    [SerializeField] private BackgroundManager Backgroundmanager;
    [Header("Light Settings")]
    public Light2D globalLight; 
    public int minRegionSizeForLight = 10;
    public float updateCooldown = 0.2f; // Seconds

    [System.Serializable]
    public class BiomeLightSetting {
        public BiomeType biome;
        public LightProperties properties;
    }
    public List<BiomeLightSetting> biomeLightConfigs;
    private Dictionary<BiomeType, LightProperties> _biomeLightSettingsMap;

    private BiomeType _currentClientBiome;
    private bool _needsUpdate = false;
    private float _lastUpdateTime = 0f;
    private Tween _colorTween;
    private Tween _intensityTween;
    // Inside LightingManager class

    // Shadows
    private GameObject _mainTilemap;
    private CompositeCollider2D _compositeCollider;

    private List<ShadowCaster2D> _activeShadowCasters = new List<ShadowCaster2D>();
    private Queue<ShadowCaster2D> _pooledShadowCasters = new Queue<ShadowCaster2D>();
    // Reusable lists to avoid allocations in the update loop
    private List<Vector2> _reusablePointsInPath = new List<Vector2>();
    private List<Vector3> _reusablePointsInPath3D = new List<Vector3>();
    private Vector3[] _pathArrayForSetter; // Reusable array for SetPathOptimized
                                           // --- Methods ---

    public override void OnStartClient() {
        base.OnStartClient();
        Initialize();
        Backgroundmanager.Init(_worldManager.WorldGenSettings,_worldManager.BiomeManager);
    }
    public override void OnStopClient() {
        base.OnStopClient();
        WorldVisibilityManager.OnLocalPlayerVisibilityChanged -= PlayerVisibilityLayerChanged;
        _worldManager.BiomeManager.OnNewClientBiome -= SetNewBiomeLight;
    }
    void Initialize() {
        // Biome stuff
        _biomeLightSettingsMap = new Dictionary<BiomeType, LightProperties>();
        foreach (var config in biomeLightConfigs) {
            if (!_biomeLightSettingsMap.ContainsKey(config.biome)) {
                _biomeLightSettingsMap.Add(config.biome, config.properties);
            }
        }
        // References
        _mainTilemap = _worldManager.GetMainTileMap();
        _compositeCollider = _mainTilemap.GetComponent<CompositeCollider2D>();

        // Subscribe to change lighting when entering interiors
        WorldVisibilityManager.OnLocalPlayerVisibilityChanged += PlayerVisibilityLayerChanged;
        _worldManager.BiomeManager.OnNewClientBiome += SetNewBiomeLight;
        // Setup starting light
        _currentClientBiome = BiomeType.Trench; // Or biome we left off at
        SetNewBiomeLightInstant(_currentClientBiome);
    }

    private void PlayerVisibilityLayerChanged(VisibilityLayerType layer) {
        if(layer == VisibilityLayerType.Interior) {
            SetLightingInterior();
            Backgroundmanager.SetInteriorBackground(true);
        } else {
            Backgroundmanager.SetInteriorBackground(false);
            // change back to what the light level was before
            SetNewBiomeLightInstant(_currentClientBiome);
        }
    }

   
    private void SetNewBiomeLight(BiomeType biomeOld, BiomeType biomeNew) {
        Debug.Log("Player entered new biome! " + biomeNew);
        // Ensure new biome exists
        if (!_biomeLightSettingsMap.TryGetValue(biomeNew, out var lighting)) {
            Debug.LogWarning($"Could not find biome: {biomeNew} in lighting data!");
            return;
        }
        // Create a sequence for smooth transitions
        Sequence transitionSequence = DOTween.Sequence();

        // Tween light intensity
        transitionSequence.Append(
            DOTween.To(
                () => globalLight.intensity,
                x => globalLight.intensity = x,
                lighting.intensity,
                5f) // Duration of 5 seconds
                .SetEase(Ease.InOutSine) // Smooth easing
        );

        // Tween light color
        transitionSequence.Join(
            DOTween.To(
                () => globalLight.color,
                x => globalLight.color = x,
                lighting.color,
                1f) // Duration of 5 seconds
                .SetEase(Ease.InOutSine) // Smooth easing
        );
        // Play the sequence
        transitionSequence.Play();
    }
    private void SetNewBiomeLightInstant(BiomeType trench) {
        if (!_biomeLightSettingsMap.TryGetValue(trench, out var lighting)) {
            Debug.LogWarning($"Could not find biome: {trench} in lighting data!");
            return;
        }
        globalLight.intensity = lighting.intensity;
        globalLight.color = lighting.color;
    }
    private void SetLightingInterior() {
        // exactly like above but just hard coded lol
        globalLight.intensity = 1;
        globalLight.color = Color.white;
    }

    public void RequestLightUpdate() {
        //Debug.Log("Updating lights!");
        _needsUpdate = true;
    }

    void Update() {
        if (_needsUpdate && Time.time > _lastUpdateTime + updateCooldown) {
            //PerformFullLightUpdateOld();
            PerformFullShadowUpdate();
            _needsUpdate = false;
            _lastUpdateTime = Time.time;
        }
    }
    void PerformFullShadowUpdate() {
        // Then it creates the new shadow casters, based on the paths of the composite collider
        int pathCount = _compositeCollider.pathCount;
        // Ensure active casters list can accommodate all paths
        // and get/create casters as needed
        for (int i = 0; i < pathCount; ++i) {
            ShadowCaster2D currentCaster;
            if (i < _activeShadowCasters.Count) {
                currentCaster = _activeShadowCasters[i];
                if (!currentCaster.gameObject.activeSelf) // Ensure it's active if reused
                {
                    currentCaster.gameObject.SetActive(true);
                }
            } else {
                currentCaster = GetShadowCasterFromPool();
                _activeShadowCasters.Add(currentCaster);
            }

            // Get path data
            _compositeCollider.GetPath(i, _reusablePointsInPath);

            // Convert List<Vector2> to Vector3[] for SetPathOptimized
            // Reuse _reusablePointsInPath3D list and _pathArrayForSetter array
            _reusablePointsInPath3D.Clear();
            for (int j = 0; j < _reusablePointsInPath.Count; ++j) {
                _reusablePointsInPath3D.Add(_reusablePointsInPath[j]); // Implicit conversion from Vector2 to Vector3 (z=0)
            }

            // Ensure our reusable array is large enough
            if (_pathArrayForSetter == null || _pathArrayForSetter.Length < _reusablePointsInPath3D.Count) {
                _pathArrayForSetter = new Vector3[_reusablePointsInPath3D.Count];
            }

            // Copy to the array segment that will be used
            for (int j = 0; j < _reusablePointsInPath3D.Count; ++j) {
                _pathArrayForSetter[j] = _reusablePointsInPath3D[j];
            }

            // If the actual path is shorter than the array, we need to pass a correctly sized array or a sub-segment.
            // SetPath likely makes a copy, but to be safe and clear, let's give it exactly what it needs.
            // A simple way if SetPathOptimized doesn't handle sub-arrays is to create the exact size.
            // However, if SetPath *does* use the array reference directly (unlikely for safety), then giving it a larger array where only a prefix is valid could be an issue.
            // Given the reflection, it's highly probable Unity's internal code for m_ShapePath copies the array.
            // So, creating a new array of the exact size here is safer and often what ToArray() does.
            // Let's stick to ToArray() for simplicity unless proven it's a major bottleneck AFTER other optimizations.
            // The previous loop created _reusablePointsInPath3D.ToArray() for each. We can optimize the array creation.

            Vector3[] finalPath;
            if (_reusablePointsInPath3D.Count == _pathArrayForSetter.Length) {
                // If counts match, we can use the array directly (assuming it was fully populated)
                finalPath = _pathArrayForSetter;
            } else {
                // If counts don't match (e.g. _pathArrayForSetter was larger and we only filled part of it),
                // or to be absolutely safe if _pathArrayForSetter isn't perfectly managed for exact size.
                finalPath = _reusablePointsInPath3D.ToArray(); // This still allocates, but only if needed or for safety.
            }
            // A more performant way if SetPath *always* copies the data:
            // No need to use _pathArrayForSetter if we always call ToArray().
            // Just use _reusablePointsInPath3D.ToArray(). The primary win is reusing the list.

            currentCaster.SetPathOptimized(finalPath); // Or _reusablePointsInPath3D.ToArray()
            currentCaster.SetPathHashOptimized(Random.Range(int.MinValue, int.MaxValue));
            // component.Update() was in your original code. Test if it's still needed.
            currentCaster.Update();
            // SetPathHash *should* trigger the internal rebuild.
            // currentCaster.Update(); // Potentially redundant. Profile this.

            // Clear the reusable list for the next path (already done at the start of the loop)
            // _reusablePointsInPath.Clear(); // Done by GetPath
        }

        // Deactivate and pool any casters that are no longer needed
        if (pathCount < _activeShadowCasters.Count) {
            for (int i = _activeShadowCasters.Count - 1; i >= pathCount; --i) {
                ReturnShadowCasterToPool(_activeShadowCasters[i]);
                _activeShadowCasters.RemoveAt(i); // Remove from the end is efficient
            }
        }
    }
    ShadowCaster2D CreateAndPoolNewShadowCaster() {
        GameObject newShadowCasterGO = new GameObject("PooledShadowCaster2D");
        newShadowCasterGO.isStatic = true; // Important: if they are static, they should not move with the parent unless the parent is also static and part of the same static batch.
                                           // If the tilemap itself moves, these children might not update correctly if marked static.
                                           // If the tilemap is static, then this is fine.
                                           // If the tilemap MOVES, then newShadowCasterGO.isStatic should be false.
        newShadowCasterGO.transform.SetParent(_compositeCollider.transform, false); // Set parent
        ShadowCaster2D component = newShadowCasterGO.AddComponent<ShadowCaster2D>();
        component.selfShadows = true; // Set this once
        newShadowCasterGO.SetActive(false); // Start inactive
        _pooledShadowCasters.Enqueue(component);
        return component;
    }
    ShadowCaster2D GetShadowCasterFromPool() {
        if (_pooledShadowCasters.Count > 0) {
            ShadowCaster2D caster = _pooledShadowCasters.Dequeue();
            caster.gameObject.SetActive(true);
            return caster;
        }
        // Pool is empty, create a new one (and it won't be added to the pool queue until returned)
        GameObject newShadowCasterGO = new GameObject("ShadowCaster2D_New");
        // Consider if isStatic is appropriate if the tilemap itself moves.
        // If the tilemap is truly static in the world, then newShadowCasterGO.isStatic = true is good.
        // If the tilemap (and thus the composite collider) can move, set isStatic = false.
        newShadowCasterGO.isStatic = true; // Or false, see comment above
        newShadowCasterGO.transform.SetParent(_compositeCollider.transform, false);
        ShadowCaster2D component = newShadowCasterGO.AddComponent<ShadowCaster2D>();
        component.selfShadows = true;
        // No need to set active true here, it will be used immediately
        return component;
    }
    void ReturnShadowCasterToPool(ShadowCaster2D caster) {
        if (caster != null) {
            caster.gameObject.SetActive(false);
            // Optionally, reset its path to an empty array or a default state if necessary
            // caster.SetPathOptimized(new Vector3[0]); 
            // caster.SetPathHashOptimized(0); // Or some default hash
            _pooledShadowCasters.Enqueue(caster);
        }
    }
}