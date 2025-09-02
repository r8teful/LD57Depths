using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Should hold server side data about the state of the different upgrades
public class SubmarineManager : NetworkBehaviour {
    // TODO Isn't it bad that we are storing the PersistentData here aswell? Should we just have a list of persistantIDs only, and then ask the entityManager for the data? 
    private Dictionary<ulong, PersistentEntityData> persistentSubEntities = new Dictionary<ulong, PersistentEntityData>();
    private Dictionary<ulong, InteriorEntityData> persistentIDToData = new Dictionary<ulong, InteriorEntityData>();
    private EntityManager _entityManager;
    private List<InteriorEntityData> interiorEntitieData; // This data gets saved
    private List<SubEntity> interiorEntities; // Runtime only data, not sure if we'll actually need this?
    public Grid SubGrid;
    // Current recipe we are working on, could derive this from _upgradeData but easier to store it like this
    private readonly SyncVar<ushort> _currentRecipe = new();
    public ushort CurrentRecipe => _currentRecipe.Value;
    // RecipeID, to its corresponding progress
    private readonly SyncDictionary<ushort, List<IDQuantity>> _upgradeData = new();
    public Dictionary<ushort, List<IDQuantity>> UpgradeData => _upgradeData.Collection;
    // A client-side event that the UI can subscribe to.
    public event Action<ushort> OnUpgradeDataChanged; // Passes the RecipeID that changed
    public static SubmarineManager Instance { get; private set; }
    private void Awake() {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }
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
            var createdEntityData = _entityManager.ServerAddNewPersistentEntity(item.id, item.pos, item.rotation, new BreakEntityData(true));
            persistentSubEntities.Add(createdEntityData.persistentId, createdEntityData);
            createdEntityData.specificData.ApplyTo(item.go);

            // Add to other dictionary
            persistentIDToData.Add(createdEntityData.persistentId, item);
        }
        InitializeUpgradeData();
        //InitLadder();
    }
    public override void OnStartClient() {
        base.OnStartClient();
        _upgradeData.OnChange += OnUpgradeDataChangeClient;
        if (!IsServerInitialized) { // Server already has the data, no need to re-process
            foreach (var state in _upgradeData) {
                OnUpgradeDataChanged?.Invoke(state.Key);
            }
        }
    }
    [Server]
    private void InitializeUpgradeData() {
        // This would pull from the save file
        foreach (var recipe in App.ResourceSystem.GetAllSubRecipes()) {
            var list = new List<IDQuantity>();
            foreach(var item in recipe.requiredItems) {
                IDQuantity iQ = new(item.item.ID, 0); // HERE, instead of 0, use the stored data!
                list.Add(iQ);
            }
            _upgradeData.Add(new(recipe.RecipeID, list));
        }
        _currentRecipe.Value = App.ResourceSystem.GetAllSubRecipes().Find(s => s.displayName == "Cables").ID; // OMG SO BAD
    }
    private void OnUpgradeDataChangeClient(SyncDictionaryOperation op, ushort key, List<IDQuantity> value, bool asServer) {
        // This callback fires on clients AND the server whenever the list changes.
        // We only care about the client-side reaction here for UI.
        if (asServer) return;
        OnUpgradeDataChanged?.Invoke(key);
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

    // This method is kind of similar to CraftinComponent AttemptCraft, but we can't use that because when we "attempthCraft" we dont want to execute
    // the recipe, only when we have all the resources contributed, if we want to use it, we'd had to have a new recipeSO for each contribution, which 
    // would just be hell, so now we have this one here
    [ServerRpc(RequireOwnership = false)] 
    public void RpcContributeToUpgrade(ushort recipeId, ushort itemId, int quantity, NetworkedPlayer client) {
        Debug.Log($"Server received contribution request for recipe {recipeId}.");

        var _clientInventory = client.GetInventory();
        var recipe = App.ResourceSystem.GetRecipeByID(recipeId);
        if(!_clientInventory.HasItemCount(itemId, quantity)) {
            Debug.LogWarning("Client doesn't have requested amount in inventory!");
            return;
        }
        _clientInventory.RemoveItem(itemId, quantity);
        // Add to resource
        int index = _upgradeData[recipeId].FindIndex(s => s.itemID == itemId);
        if (index == -1) {
            Debug.LogWarning("Coudn't find valid item idex!");
            return;
        }
        var list = _upgradeData[recipeId][index];
        list.quantity += quantity;
        //_upgradeData[recipeId][index] = list; // Below line should be a more performant way of this line
        _upgradeData.Dirty(recipeId);
    }

    internal int GetUpgradeIndex(ushort curRecipe) {
        // If the upgradeData has the required amount of resources, then it passes
        var recipeData = App.ResourceSystem.GetRecipeByID(curRecipe);
        var reqItemList = recipeData.requiredItems;
        int upgradeIndex = 0;
        if (UpgradeData.TryGetValue(curRecipe, out var list)){
            foreach (var item in list) {
                var index = reqItemList.FindIndex(i => i.item.ID == item.itemID);
                if (index != -1) {
                    // now we now how many resource we have in the data, and how many resources we need in the recipe, of that same resource
                    if (reqItemList[index].quantity <= item.quantity) {
                        upgradeIndex++;
                    }
                }
            }
        }
        return upgradeIndex;
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