using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class UICraftingManager : Singleton<UICraftingManager> {
    public Transform recipeListContainer; // Parent for recipe UI elements
    public GameObject recipeUIPrefab;    // Prefab for displaying a single recipe
    private List<CraftingRecipeSO> _availableRecipes = new List<CraftingRecipeSO>();
    private InventoryManager _clientInventory; // Your existing client inventory manager
    private PopupManager _popupManager;
    public void Init(InventoryManager clientInv,PopupManager popupManager) {
        _clientInventory = clientInv;
        _popupManager = popupManager;
        _availableRecipes = App.ResourceSystem.GetAllCraftingRecipes();
        // PopupManager should be on same component
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
                displayItem.Init(recipe, _clientInventory, this);
                _popupManager.RegisterIPopupInfo(displayItem);
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
}