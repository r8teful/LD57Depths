using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
public class GrowableEntity : NetworkBehaviour {
    #region Events
    /// <summary>
    /// Called on the SERVER when this entity advances to the next growth stage.
    /// Passes the instance that grew.
    /// </summary>
    public static event Action<GrowableEntity> OnServerGrowthStageAdvanced;
    /// <summary>
    /// Called on the CLIENT when this entity, visible to the client, advances to the next growth stage.
    /// Passes the instance that grew.
    /// </summary>
    public static event Action<GrowableEntity> OnClientGrowthStageAdvanced;
    /// <summary>
    /// Called on the SERVER when this entity is spawned.
    /// Passes the instance that spawned
    /// </summary>
    #endregion

    [SerializeField]
    private GrowthSO _growData;
    private readonly SyncVar<int> _currentGrowthStage = new SyncVar<int>();
    // --- Server-Only State ---
    private float _growthTimer;

    // --- Client-Only State ---
    [SerializeField] SpriteRenderer _spriteRenderer;


    #region Public Properties
    public int CurrentStage => _currentGrowthStage.Value;
    public bool IsFullyGrown => _currentGrowthStage.Value >= _growData.StageSeconds.Length - 1;
    public GrowthSO GrowData => _growData;
    #endregion

    private void Start() {
        _currentGrowthStage.OnChange += OnGrowthStageChanged;
    }
    public override void OnStartServer() {
        base.OnStartServer();
        // Initialize state on the server when spawned
        _growthTimer = 0f;
    }
 

    public override void OnStartClient() {
        base.OnStartClient();
        // Update visuals to the initial state when the object spawns on a client.
        // This is important for late-joining players.
        UpdateVisuals(_currentGrowthStage.Value);
    }

    private void OnGrowthStageChanged(int prev, int next, bool asServer) {
        // Update the visuals to match the new stage
        UpdateVisuals(next);

        // Notify local managers that this entity has grown.
        // We check !asServer to ensure we don't fire this for the host/server acting as a client.
        // The server has its own dedicated event.
        if (!asServer) {
            OnClientGrowthStageAdvanced?.Invoke(this);
        }
    }

    private void Update() {
        // The core growth logic ONLY runs on the server.
        if (!IsServerInitialized)
            return;

        if (IsFullyGrown)
            return;

        _growthTimer += Time.deltaTime;

        // Check if enough time has passed to advance to the next stage
        if (_growthTimer >= _growData.StageSeconds[_currentGrowthStage.Value]) {
            AdvanceStage_Server();
        }
    }

    [Server]
    private void AdvanceStage_Server() {
        if (IsFullyGrown) return;

        _growthTimer = 0f; // Reset timer for the next stage
        _currentGrowthStage.Value++; // This change will trigger the OnChange hook on all clients

        // Fire the server-side event for server systems to listen to
        Debug.Log($"Server: {gameObject.name} grew to stage {_currentGrowthStage}. Firing server event.");
        OnServerGrowthStageAdvanced?.Invoke(this);
    }

    /// <summary>
    /// Handles instantiating and cleaning up the visual representation for the current growth stage.
    /// </summary>
    private void UpdateVisuals(int stageIndex) {
        if (_growData == null || _growData.StageSprites.Length == 0) return;

        // Ensure index is valid
        stageIndex = Mathf.Clamp(stageIndex, 0, _growData.StageSprites.Length - 1);

        // Create the new visual
        Sprite sprite = _growData.StageSprites[stageIndex];
        if (sprite != null) {
            _spriteRenderer.sprite = sprite; 
        }

        Debug.Log($"Client/Visuals: {gameObject.name} updated visuals to stage {stageIndex}.");
    }
}