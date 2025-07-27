using FishNet.Object;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Should hold server side data about the state of the different upgrades
public class SubInterior : NetworkBehaviour {
    private Dictionary<ulong, PersistentEntityData> persistentSubEntities = new Dictionary<ulong, PersistentEntityData>();
    private Dictionary<ulong, InteriorEntityData> persistentIDToData = new Dictionary<ulong, InteriorEntityData>();
    private EntityManager _entityManager;
    private List<InteriorEntityData> interiorEntitieData; // This data gets saved
    private List<SubEntity> interiorEntities; // Runtime only data, not sure if we'll actually need this?
    public Grid SubGrid;

    // TODO We'll have to change this when we're adding several subs but wont be in a while
    public static SubInterior Instance { get; private set; }
    public override void OnStartServer() {
        base.OnStartServer();
        // TODO you'll first have to LOAD the existing server entity data, if it doesn't exist, then only create the new ones
        // You'll only create new entities a few times, like when starting the game for the first time.
        // Or maybe later when you unlock a new area or interior. something like that
        _entityManager = EntityManager.Instance;
        interiorEntitieData = new List<InteriorEntityData>();

        var interior = GetAllInteriorEntities();
        interiorEntities = interior.Item2;
        interiorEntitieData = interior.Item1;
        foreach (var item in interiorEntitieData) {
            var createdEntityData = _entityManager.ServerAddNewPersistentSubEntity(item.id, item.pos, item.rotation);
            persistentSubEntities.Add(createdEntityData.persistentId, createdEntityData);
            createdEntityData.specificData.ApplyTo(item.go);

            // Add to other dictionary
            persistentIDToData.Add(createdEntityData.persistentId, item);
        }
        //InitLadder();
    }
    private void Awake() {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    // Fuck this we just enable the ladder from the start
    private void InitLadder() {
        // This is stupid but the ladder should not be interactable until we have fixed the control pannel 
        Debug.Log("persistentSubEntities: " + persistentSubEntities);
        var firstMatch = persistentSubEntities.Values.FirstOrDefault(entity => entity.entityID == ResourceSystem.LadderID); 
        if (firstMatch != null) {
            if (persistentIDToData.TryGetValue(firstMatch.persistentId, out var v)) {
                v.go.GetComponent<IInteractable>().CanInteract = false;
            }
        } else {
            Debug.LogError("Could not find ladder");
        }
    }

    private (List<InteriorEntityData>,List<SubEntity>) GetAllInteriorEntities() {
        List<InteriorEntityData> interiorData = new List<InteriorEntityData>();
        List<SubEntity> interiorEntities = new List<SubEntity>();
        var subEntities = GetComponentsInChildren<SubEntity>();
        if (subEntities != null) {
            foreach (var entity in subEntities) {
                Vector3Int cellPos = SubGrid.WorldToCell(entity.transform.position);
                interiorData.Add(new InteriorEntityData(entity.gameObject, entity.EntityData.ID, cellPos, entity.transform.rotation));
                interiorEntities.Add(entity);
            }
        }
        return (interiorData,interiorEntities);
    }

    // Not sure how we're going to handle interaction state through the network but we just have to make sure this is run on all the clients

    // CanInteract must be a network variable here,  how else will we know if the ladder is interactable?
    [ServerRpc(RequireOwnership = false)]
    internal void SetLadderActiveRpc() {
        Debug.Log("persistentSubEntities: " + persistentSubEntities);
        var firstMatch = persistentSubEntities.Values.FirstOrDefault(entity => entity.entityID == ResourceSystem.LadderID);
        if (firstMatch != null) {
            if (persistentIDToData.TryGetValue(firstMatch.persistentId, out var v)) {
                v.go.GetComponent<IInteractable>().CanInteract = true;
            }
        } else {
            Debug.LogError("Could not find ladder");
        }
    }
}

internal struct InteriorEntityData {
    public GameObject go;
    public ushort id;
    public Vector3Int pos;
    public Quaternion rotation;

    public InteriorEntityData(GameObject go, ushort id, Vector3Int pos, Quaternion rotation) {
        this.go = go;
        this.id = id;
        this.pos = pos;
        this.rotation = rotation;
    }
}