using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.PackageManager;
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
    private readonly SyncVar<int> _currentZoneIndex = new();
    public ushort CurrentRecipe => _currentRecipe.Value;
    public int CurrentZoneIndex => _currentZoneIndex.Value;
    // RecipeID, to its corresponding progress
    private readonly SyncDictionary<ushort, List<IDQuantity>> _upgradeData = new();
    public Dictionary<ushort, List<IDQuantity>> UpgradeData => _upgradeData.Collection;
    // A client-side event that the UI can subscribe to.
    public event Action<ushort> OnUpgradeDataChanged; // Passes the RecipeID that changed
    public event Action<ushort> OnCurRecipeChanged; // Passes the new RecipeID 
    public event Action OnSubMoved; // Used by map UI 

    public static SubmarineManager Instance { get; private set; }
    internal void SetSubPosIndex(int index) {
        _currentZoneIndex.Value = index;
    }
    private void Awake() {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        _currentZoneIndex.OnChange += OnZoneChange;
        _currentRecipe.OnChange += OnRecipeChange;
    }

    private void OnRecipeChange(ushort prev, ushort next, bool asServer) {
        OnCurRecipeChanged?.Invoke(next);
    }

    private void OnZoneChange(int prev, int next, bool asServer) {
        OnSubMoved?.Invoke();
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
                IDQuantity iQ = new(item.item.ID, 0); // TODO HERE, instead of 0, use the stored data!
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
        if (key == 0) return;

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
    public void AttemptContribute(ushort recipeId, ushort itemId, int quantity) {
        // Handle the removing locally because server can't acces a remote player inventory
        var _clientInventory = NetworkedPlayer.LocalInstance.GetInventory();
        if (!_clientInventory.HasItemCount(itemId, quantity)) {
            Debug.LogWarning("Client doesn't have requested amount in inventory!");
            return;
        }
        _clientInventory.RemoveItem(itemId, quantity);
        RpcContributeToUpgrade(recipeId, itemId, quantity);
    }
    // This method is kind of similar to CraftinComponent AttemptCraft, but we can't use that because when we "attemptCraft" we dont want to execute
    // the recipe, only when we have all the resources contributed, if we want to use it, we'd had to have a new recipeSO for each contribution, which 
    // would just be hell, so now we have this one here
    [ServerRpc(RequireOwnership = false)] 
    public void RpcContributeToUpgrade(ushort recipeId, ushort itemId, int quantity) {
        //Debug.Log($"Server received contribution request for recipe {recipeId}.");
        var recipe = App.ResourceSystem.GetRecipeByID(recipeId);
        // Add to resource
        int upgradeDataIndex = _upgradeData[recipeId].FindIndex(s => s.itemID == itemId);
        int recipeIndex = recipe.requiredItems.FindIndex(s => s.item.ID == itemId);
        if (upgradeDataIndex == -1 || recipeIndex == -1) {
            Debug.LogWarning("Coudn't find valid item idex!");
            return;
        }
        var list = _upgradeData[recipeId][upgradeDataIndex];
        list.quantity += quantity;

        _upgradeData[recipeId][upgradeDataIndex] = list;  // Set the data
        
        // Now that the data is set, it should be locally alswell, we can do the check, because the list is changed
        if(list.quantity >= recipe.requiredItems[recipeIndex].quantity) {
            // Reached the full quantity! 
            list.quantity = recipe.requiredItems[recipeIndex].quantity;
            if (IsAllStagesDone(recipe.ID)) {
                Debug.Log("All recipe stages done"!);
                // play sound effect idk? 
                StartNextUpgrade();
                return;
            }
        }

        _upgradeData.Dirty(recipeId); // This will call the OnChangeEvent
    }

    private void StartNextUpgrade() {
        // Check if next recipe data exists
        var r = App.ResourceSystem.GetRecipeByID((ushort)(_currentRecipe.Value + 1));
        if (r == null) {
            Debug.LogWarning("No more recipe data found, maybe we reached final upgrade?");
            return;
        }
        // Todo add a slight delay, visual stuff, sounds, etc
        _currentRecipe.Value++;
        //OnCurRecipeChanged?.Invoke(_currentRecipe.Value); // Moved to OnChange because then it runs on all clients
    }

    private bool IsAllStagesDone(ushort recipeID) {
        var recipeData = App.ResourceSystem.GetRecipeByID(recipeID);
        var uIndex = GetUpgradeIndex(recipeID); // Just use this function lol
        if (uIndex < recipeData.requiredItems.Count) return false;
        return true;
    }
    private int GetUpgradeStage() {
        // ID is sequential so we just take the current minus the first and we get the stage we have! lol
        return CurrentRecipe - ResourceSystem.FIRST_SHIP_RECIPE_ID;
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
    public int GetContributedAmount(ushort recipeID, ushort itemID) {
        if (_upgradeData.TryGetValue(recipeID, out var list)) {
            int index = list.FindIndex(r => r.itemID == itemID);
            if (index == -1) return -1;
            return list[index].quantity;
        }
        return -1;
    }

    internal bool CanMoveTo(int currentShownIndex) {
        return GetUpgradeStage() >= currentShownIndex;
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