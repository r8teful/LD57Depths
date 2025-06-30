using FishNet.Object;
using System.Collections.Generic;
using UnityEngine;

// Should hold server side data about the state of the different upgrades
public class SubInterior : NetworkBehaviour {
    private Dictionary<ulong, PersistentEntityData> persistentSubEntities = new Dictionary<ulong, PersistentEntityData>();
    private Dictionary<ulong, InteriorEntityData> persistentIDToData = new Dictionary<ulong, InteriorEntityData>();
    private EntityManager _entityManager;
    private List<InteriorEntityData> interiorEntities;
    public Grid SubGrid;
    public override void OnStartServer() {
        base.OnStartServer();
        // TODO you'll first have to LOAD the existing server entity data, if it doesn't exist, then only create the new ones
        // You'll only create new entities a few times, like when starting the game for the first time.
        // Or maybe later when you unlock a new area or interior. something like that
        _entityManager = EntityManager.Instance;
        interiorEntities = new List<InteriorEntityData>();
        interiorEntities = GetAllInterioEntities();
        foreach (var item in interiorEntities) {
            var createdEntityData = _entityManager.ServerAddNewPersistentSubEntity(item.id, item.pos, item.rotation);
            persistentSubEntities.Add(createdEntityData.persistentId, createdEntityData);
            createdEntityData.specificData.ApplyTo(item.go);

            // Add to other dictionary
            persistentIDToData.Add(createdEntityData.persistentId, item);
        }
    }

    private List<InteriorEntityData> GetAllInterioEntities() {
        List<InteriorEntityData> data = new List<InteriorEntityData>();
        var subEntities = GetComponentsInChildren<SubEntity>();
        if (subEntities != null) {
            foreach (var entity in subEntities) {
                Vector3Int cellPos = SubGrid.WorldToCell(entity.transform.position);
                data.Add(new InteriorEntityData(entity.gameObject, entity.EntityData.ID, cellPos, entity.transform.rotation));
            }
        }
        return data;
    }

    public void FixEntity(ulong persistentEntityID) {
        persistentSubEntities[persistentEntityID].specificData.ApplyTo(persistentIDToData[persistentEntityID].go);
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