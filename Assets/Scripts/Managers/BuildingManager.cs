using System;
using System.Collections;
using UnityEngine;
// Handles building of entities, and later probably building of sub rooms using the same system
public class BuildingManager : Singleton<BuildingManager> {
    public Action<bool> OnBuildAttemptComplete;
    public bool IsBuilding { get; private set; }
    private PlaceableEntity _currentPlacingEntity;

    private void Update() {
        if (IsBuilding && _currentPlacingEntity != null) {
            UpdateEntityVisual();
            UpdateEntityPosition();
        }
    }

    private void UpdateEntityPosition() {
        if (_currentPlacingEntity == null) return;

        var player = NetworkedPlayer.LocalInstance;
        if(player == null) {
            Debug.LogError("Could not find NetworkedPlayer");
            return;
        }
        // Get mouse position in screen space
        Vector3 mouseWorldPos = player.InputManager.GetAimWorldInput();
        mouseWorldPos.z = 0f; // Ensure z is 0 for 2D placement

        // Snap to grid using WorldManager's tilemap
        Vector3Int cellPos = WorldManager.Instance.WorldToCell(mouseWorldPos);
        Vector3 snappedWorldPos = WorldManager.Instance.GetCellCenterWorld(cellPos);

        // Set the entity's position to the snapped position
        _currentPlacingEntity.transform.position = snappedWorldPos;
    }

    private void UpdateEntityVisual() {
        if (CanPlaceEntity()) {
            // Green
            _currentPlacingEntity.SetColor(Color.green);
        } else {
            // Red
            _currentPlacingEntity.SetColor(Color.red);
        }
    }

    public void EnterBuilding(ushort entityID) {
        // Try and enter building mode first
        if (!TryEnterBuilding()) {
            Debug.LogWarning("Failed to enter build mode!");
            OnBuildAttemptComplete?.Invoke(false);
            return;
        }
        // We are now in building mode
        // Spawn a preview of the entity, which is really just the entity already, because why not?
        var entity = App.ResourceSystem.GetEntityByID(entityID);
        if (entity.entityPrefab != null) { // Safety
            var g = Instantiate(entity.entityPrefab);
            if (g.TryGetComponent<PlaceableEntity>(out var placeableEntity)) {
                _currentPlacingEntity = placeableEntity; // This is a dummy now 
            } else {
                Debug.LogError("Placeable entity needs PlaceAbleEntity Script!");
            }
        }
        IsBuilding = true;
    }

    public bool TryEnterBuilding() {
        var player = NetworkedPlayer.LocalInstance;
        if (player == null) {
            Debug.LogError("Could not find NetworkedPlayer");
            return false;
        }
        if (!player.InputManager.TryEnterBuildMode()) {
            return false;
        }
        // Posible other checks
        return true;
    }
    // TODO here its not working
    private bool CanPlaceEntity() {
        if(_currentPlacingEntity == null) return false; // Safety
        
        Collider2D[] results = new Collider2D[10]; 
        ContactFilter2D filter = new ContactFilter2D().NoFilter(); // We could set filters here later
        int overlapCount = Physics2D.OverlapCollider(_currentPlacingEntity.PlacementCollider, filter, results);
        if (overlapCount > 0) {
            //Debug.Log("Collider overlaps with " + overlapCount + " objects.");
            return false;
        } else {
            return true;
        }
    }
    public void ExitBuild() {
        _currentPlacingEntity = null;
        IsBuilding = false;
    }
    internal void UserPlacedClicked() {
        if (CanPlaceEntity()) {
            HandlePlaceSuccess();
        } else {
            HandlePlaceFailed();
        }
    }

    private void HandlePlaceFailed() {
        Debug.Log("Can't place!");
        OnBuildAttemptComplete?.Invoke(false);
    }
    private void HandlePlaceSuccess() {
        PlaceEntity();
        ExitBuild();
        OnBuildAttemptComplete?.Invoke(true);
    }

    private void PlaceEntity() {
        _currentPlacingEntity.SetColor(Color.white); // color to "normal"
    }
}