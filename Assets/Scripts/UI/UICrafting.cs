using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UICrafting : MonoBehaviour {
    public Transform recipeListContainer; // Parent for recipe UI elements
    public GameObject recipeUIPrefab;    // Prefab for displaying a single recipe
    
    public TextMeshProUGUI statusText; // For displaying craft success/failure
    private InventoryUIManager _parentManager;
    private List<RecipeBaseSO> _availableRecipes = new List<RecipeBaseSO>();
    private InventoryManager _clientInventory; // Your existing client inventory manager

    public void Init(InventoryManager clientInv) {
        _clientInventory = clientInv;
        _availableRecipes = App.ResourceSystem.GetAllRecipes();

        // Subscribe to inventory updates to refresh UI
        _clientInventory.OnSlotChanged += RefreshRecipeDisplayStatus;
        // Subscribe to craft results
        CraftingManager.OnCraftSuccessClient += HandleCraftSuccess;
        CraftingManager.OnCraftFailClient += HandleCraftFail;
        PopulateRecipeList();
    }

    void OnDestroy() {
        if (_clientInventory != null) {
            _clientInventory.OnSlotChanged -= RefreshRecipeDisplayStatus;
        }
        CraftingManager.OnCraftSuccessClient -= HandleCraftSuccess;
        CraftingManager.OnCraftFailClient -= HandleCraftFail;
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

    public void AttemptCraft(RecipeBaseSO recipe) {
        if (recipe == null || _clientInventory == null)
            return;

        // Client-side check (optional, good for immediate feedback but server is authoritative)
        if (!recipe.CanClientAfford(_clientInventory)) {
            Debug.Log($"Cannot afford {recipe.displayName} (client check).");
            if (statusText)
                statusText.text = $"Cannot afford {recipe.displayName}";
            // You might still allow the request to go to server for a server-denial message
        }

        // Tell the local InventoryManager (or CraftingManager client instance) to send the request
        CraftingManager.Instance.ClientRequestCraft(recipe.RecipeID);
        if (statusText)
            statusText.text = $"Attempting to craft {recipe.displayName}...";
    }

    private void HandleCraftSuccess(ushort recipeID) {
        RecipeBaseSO recipe = _availableRecipes.Find(r => r.RecipeID == recipeID);
        string name = recipe != null ? recipe.displayName : recipeID.ToString();
        Debug.Log($"UI: Successfully crafted {name}!");
        if (statusText)
            statusText.text = $"Successfully crafted {name}!";
        RefreshRecipeDisplayStatus(0); // Refresh UI as inventory has changed
    }

    private void HandleCraftFail(ushort recipeID, string reason) {
        RecipeBaseSO recipe = _availableRecipes.Find(r => r.RecipeID == recipeID);
        string name = recipe != null ? recipe.displayName : recipeID.ToString();
        Debug.LogError($"UI: Failed to craft {name}. Reason: {reason}");
        if (statusText)
            statusText.text = $"Failed to craft {name}. Reason: {reason}";
        RefreshRecipeDisplayStatus(0); // Refresh in case some partial state needs updating
    }
}