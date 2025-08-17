using FishNet.Object;
using System;
using System.Collections;
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;
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
            _currentPlacingEntity.SetColor(new(0,1,0,0.8f)); // Green
        } else {
            _currentPlacingEntity.SetColor(new(1,0,0,0.8f)); // Red 
        }
    }

    public void EnterBuilding(PlaceableEntity entityGameObject) {
        // Try and enter building mode first
        if (!TryEnterBuilding()) {
            Debug.LogWarning("Failed to enter build mode!");
            OnBuildAttemptComplete?.Invoke(false);
            return;
        }
        // We are now in building mode, close inventory
        
        NetworkedPlayer.LocalInstance.UiManager.UIManagerInventory.HandleToggleInventory(); // This seems very bad but eh?

        // Spawn a preview of the entity
        _currentPlacingEntity = Instantiate(entityGameObject);
        IsBuilding = true; // Set flag to true, now update function will handle the rest
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
        Destroy(_currentPlacingEntity.gameObject);
        _currentPlacingEntity = null;
        IsBuilding = false;
    }
    internal void UserPlacedClicked(NetworkObject client) {
        if (CanPlaceEntity()) {
            HandlePlaceSuccess(client); // Need to pass client in order to spawn it properly
        } else {
            // We don't actually exit building mode here, maybe an error sound? Or some kind of feedback
            //HandlePlaceFailOrCancel();
        }
    }
    private void HandlePlaceSuccess(NetworkObject client) {
        PlaceEntity(client);
        ExitBuild();
        Debug.Log("Place Sucess calling event");
        // When this event is called, it somehow ONLY passes when we didn't close the invenoty
        OnBuildAttemptComplete?.Invoke(true);
    }
    public void HandlePlaceFailOrCancel() {
        ExitBuild();
        OnBuildAttemptComplete?.Invoke(false);
    }

    private void PlaceEntity(NetworkObject client) {
        // Spawn the actual "real" entity on the server
        var entity = _currentPlacingEntity.EntityData;
        if (entity == null) {
            Debug.LogError("Entity prefab data not found!");
            return;
        }
        // Get final position 
        var pos = _currentPlacingEntity.transform.position;
        //Vector3Int p = new Vector3Int(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z));
        //Debug.Log($"Pos: {pos} placing object at: {p}");
        Vector3Int p = WorldManager.Instance.WorldToCell(pos);
        EntitySpecificData data = CreateEntityByID(entity.ID);
        EntityManager.Instance.AddAndSpawnEntityForClient(entity.entityID, p, Quaternion.identity, client.LocalConnection, data);
        // Despawn preview is handled in ExitBuild()
    }
    private EntitySpecificData CreateEntityByID(ushort id) {
        if (ResourceSystem.IsOxygenMachineID(id)) {
            return new OxygenEntityData(4.25f);
        } else if (ResourceSystem.IsLightID(id)) {
            return  new LightEntityData(4.25f);
        }
        return null;
    }
}