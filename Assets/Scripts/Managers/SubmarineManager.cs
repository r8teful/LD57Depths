using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

// This is on the interior instance
public class SubmarineManager : StaticInstance<SubmarineManager> {
    // Current recipe we are working on, could derive this from _upgradeData but easier to store it like this
    private ushort _currentRecipe;
    private int _currentZoneIndex;
    public ushort CurrentRecipe => _currentRecipe;
    public int CurrentZoneIndex => _currentZoneIndex;
    private Dictionary<ushort, List<IDQuantity>> _upgradeData = new();
    public Dictionary<ushort, List<IDQuantity>> UpgradeData => _upgradeData;
    // A client-side event that the UI can subscribe to.
    public event Action<ushort> OnUpgradeDataChanged; // Passes the RecipeID that changed
    public event Action<ushort> OnCurRecipeChanged; // Passes the new RecipeID 
    public event Action OnSubMoved; // Used by map 
    public GameObject submarineExterior;
    public Transform InteriorSpawnPoint;
    [ShowInInspector]
    private InventoryManager subInventory;
    public InventoryManager SubInventory => subInventory;
    [SerializeField] private SubItemGainVisualSpawner itemGainSpawner;


    protected override void Awake() {
        base.Awake();
        subInventory = new InventoryManager();
        itemGainSpawner.Init(subInventory);
    }
    public void MoveSub(int index) {
        submarineExterior.transform.position = new(0, GameSetupManager.Instance.WorldGenSettings.GetWorldLayerYPos(index));
        SetSubPosIndex(index);
    }
    internal void SetSubPosIndex(int index) {
        _currentZoneIndex = index;
        OnSubMoved?.Invoke();
    }

    public void Start() {
        InitializeUpgradeData();
        var y = GameSetupManager.Instance.WorldGenSettings.MaxDepth;
        submarineExterior.transform.position = new Vector3(0, y);
    }
    
    private void InitializeUpgradeData() {
        // This would pull from the save file
        foreach (var recipe in App.ResourceSystem.GetAllSubRecipes()) {
            var list = new List<IDQuantity>();
            foreach(var item in recipe.requiredItems) {
                IDQuantity iQ = new(item.item.ID, 0); // TODO HERE, instead of 0, use the stored data!
                list.Add(iQ);
            }
            _upgradeData.Add(recipe.RecipeID, list);
        }
        _currentRecipe = App.ResourceSystem.GetAllSubRecipes().Find(s => s.displayName == "Cables").ID; // OMG SO BAD
        OnCurRecipeChanged?.Invoke(_currentRecipe);
    }

    public void AttemptContribute(ushort recipeId, ushort itemId, int quantity) {
        // Handle the removing locally because server can't acces a remote player inventory
        var _clientInventory = PlayerManager.LocalInstance.GetInventory();
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
            }
        }
        OnUpgradeDataChanged?.Invoke(recipeId);
    }

    private void StartNextUpgrade() {
        // Check if next recipe data exists
        var r = App.ResourceSystem.GetRecipeByID((ushort)(_currentRecipe + 1));
        if (r == null) {
            Debug.LogWarning("No more recipe data found, maybe we reached final upgrade?");
            return;
        }
        // Todo add a slight delay, visual stuff, sounds, etc
        _currentRecipe++;
        OnCurRecipeChanged?.Invoke(_currentRecipe);
    }

    private bool IsAllStagesDone(ushort recipeID) {
        var recipeData = App.ResourceSystem.GetRecipeByID(recipeID);
        var uIndex = GetUpgradeIndex(recipeID); // Just use this function lol
        if (uIndex < recipeData.requiredItems.Count) return false;
        return true;
    }
    public int GetUpgradeStage() {
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

    internal void MoveInterior(VisibilityLayerType currentLayer) {
        if (currentLayer == VisibilityLayerType.Interior) {
            // Move in
            transform.position = submarineExterior.transform.position;
        } else {
            // Move out lol
            Vector3 YEET = new(6000, 0);
            transform.position = submarineExterior.transform.position + YEET;
        }
    }
}