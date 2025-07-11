using FishNet.Object;
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
    private CraftingComponent _craftingComponent;
    public override void OnStartServer() {
        base.OnStartServer();
        // TODO you'll first have to LOAD the existing server entity data, if it doesn't exist, then only create the new ones
        // You'll only create new entities a few times, like when starting the game for the first time.
        // Or maybe later when you unlock a new area or interior. something like that
        _entityManager = EntityManager.Instance;
        _craftingComponent = gameObject.AddComponent<CraftingComponent>();// I guess this works? We need a way to execute the fixing of the enteriors, we could make a separate FixableManager script or something but we could just have this one here
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
        InitSubEntities();
    }

    private void InitSubEntities() {
        foreach (var entity in interiorEntities) {
            if(entity.TryGetComponent<FixableEntity>(out var e)) {
                e.InitParent(this); // Not sure about this, it would be better if we set it within the specificData but this works?
            }
        }
        InitLadder();
    }

    private void InitLadder() {
        // This is stupid but the ladder should not be interactable until we have fixed the control pannel 
        var firstMatch = persistentSubEntities.Values.FirstOrDefault(entity => entity.entityID == 501); // 501 is ladder lol
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

    public void TryFixEntity(RecipeBaseSO fixRecipe, UIPopup instantatiatedPopup, RecipeExecutionContext context) {
        _craftingComponent.AttemptCraft(fixRecipe, context, instantatiatedPopup);
    }

    public void EntityFixed(FixableEntity fixableEntity) {
        // If this is called the object should have a subentity attached to it
        if(fixableEntity.TryGetComponent<SubMachine>(out var ent)){
            if(ent.type == SubMachineType.ControlPanell) {
                // control panell fixed! Enable ladder next
                var firstMatch = persistentSubEntities.Values.FirstOrDefault(entity => entity.entityID == 5); // 5 is ladder lol
                if(firstMatch != null) {
                    if(persistentIDToData.TryGetValue(firstMatch.persistentId, out var v)){
                        v.go.GetComponent<IInteractable>().CanInteract = true;
                    }
                } else {
                    Debug.LogError("Could not find ladder");
                }
            }
        } else {
            Debug.LogError("Entity does not have a SubMachine component attached!");
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