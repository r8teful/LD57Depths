using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UICraftingManager : Singleton<UICraftingManager> {
    public Transform recipeListContainer; // Parent for recipe UI elements
    public GameObject recipeUIPrefab;    // Prefab for displaying a single recipe
    
    public TextMeshProUGUI statusText; // For displaying craft success/failure
    private InventoryUIManager _parentManager;
    private List<CraftingRecipeSO> _availableRecipes = new List<CraftingRecipeSO>();
    private InventoryManager _clientInventory; // Your existing client inventory manager

    public void Init(InventoryManager clientInv) {
        _clientInventory = clientInv;
        _availableRecipes = App.ResourceSystem.GetAllCraftingRecipes();

        // Subscribe to inventory updates to refresh UI
        _clientInventory.OnSlotChanged += RefreshRecipeDisplayStatus;
        // Subscribe to craft results;
        PopulateRecipeList();
    }

    void OnDestroy() {
        if (_clientInventory != null) {
            _clientInventory.OnSlotChanged -= RefreshRecipeDisplayStatus;
        }
    }

    void PopulateRecipeList() {
        foreach (Transform child in recipeListContainer) {
            Destroy(child.gameObject);
        }

        foreach (var recipe in _availableRecipes) {
            GameObject recipeGO = Instantiate(recipeUIPrefab, recipeListContainer);
            // Assuming your recipeUIPrefab has a script like 'RecipeDisplayItem.cs'
            UIRecipeItem displayItem = recipeGO.GetComponent<UIRecipeItem>();
            if (displayItem != null) {
                displayItem.Init(recipe, _clientInventory, this,GetComponent<PopupManager>()); // PopupManager should be on same component
            }
        }
        RefreshRecipeDisplayStatus(0); // Initial status update
    }

    // i is the index of the slot that has changed, optional to use it, we're just updating everything now
    public void RefreshRecipeDisplayStatus(int i) {
        if (_clientInventory == null)
            return;
        foreach (Transform child in recipeListContainer) {
            UIRecipeItem displayItem = child.GetComponent<UIRecipeItem>();
            if (displayItem != null) {
                displayItem.UpdateStatus(_clientInventory);
            }
        }
    }

    public void AttemptCraft(RecipeBaseSO recipe, UIPopup instantatiatedPopup, RecipeExecutionContext context) {
        // TODO possible use client inventoy from context here. But no, we don't really want to change that, or have other scripts store it, just have it be stored here and create a new context each time
        // We call ExecuteRecipe
        if (recipe == null || _clientInventory == null)
            return;
        if(context == null) {
            // Popuplate it with
            context = new RecipeExecutionContext { PlayerInventory = _clientInventory };
        }
        // Client-side check (optional, good for immediate feedback but server is authoritative)
        if (!recipe.CanAfford(_clientInventory)) {
            Debug.Log($"Cannot afford {recipe.displayName} (client check).");
            if (statusText)
                statusText.text = $"Cannot afford {recipe.displayName}";
            HandleCraftFail(recipe, instantatiatedPopup,$"Cannot afford {recipe.displayName}");
            return;
        }
        // Craft the bitch, first remove items
        _clientInventory.ConsumeItems(recipe.requiredItems);
        // 2. Resources consumed. Now execute the recipe outcome.
        bool executionSuccess = recipe.ExecuteRecipe(context);
        if(executionSuccess) {
            HandleCraftSuccess(recipe);
        } else {
            HandleCraftFail(recipe, instantatiatedPopup, "Unable to craft!");
        }
        // Tell the local InventoryManager (or CraftingManager client instance) to send the request
        if (statusText)
            statusText.text = $"Attempting to craft {recipe.displayName}...";
    }

    private void HandleCraftSuccess(RecipeBaseSO recipe) {
        string name = recipe != null ? recipe.displayName : recipe.ID.ToString();
        Debug.Log($"UI: Successfully crafted {name}!");
        if (statusText)
            statusText.text = $"Successfully crafted {name}!";
        RefreshRecipeDisplayStatus(0); // Refresh UI as inventory has changed
    }

    private void HandleCraftFail(RecipeBaseSO recipe, UIPopup instantatiatedPopup, string reason) {
        string name = recipe != null ? recipe.displayName : recipe.ID.ToString();
        Debug.Log($"UI: Failed to craft {name}. Reason: {reason}");
        instantatiatedPopup.HandleFailVisual();
        if (statusText)
            statusText.text = $"Failed to craft {name}. Reason: {reason}";
        RefreshRecipeDisplayStatus(0); // Refresh in case some partial state needs updating
    }
}