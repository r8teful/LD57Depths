using System.Collections;
using UnityEngine;

// Common logic needed for various crafting 
public class CraftingComponent : MonoBehaviour {
    private InventoryManager _clientInventory; // Your existing client inventory manager
    private PopupManager _popupManager;

    public void Init(InventoryManager clientInv) {
        _clientInventory = clientInv;
        _popupManager = GetComponent<PopupManager>();
    }
    public void AttemptCraft(RecipeBaseSO recipe, UIPopup instantatiatedPopup, RecipeExecutionContext context) {
        // TODO possible use client inventoy from context here. But no, we don't really want to change that, or have other scripts store it, just have it be stored here and create a new context each time
        // We call ExecuteRecipe
        Debug.Log("AttemptCraft!");
        if (recipe == null || _clientInventory == null)
            return;
        if (context == null) {
            // Popuplate it with
            context = new RecipeExecutionContext { PlayerInventory = _clientInventory };
        }
        if (instantatiatedPopup == null) {
            // Just take the current popup from the popupManager
            instantatiatedPopup = _popupManager.CurrentPopup;
        }
        // Client-side check, WE ARE NOT CHECKING ON SERVER NOW!!
        if (!recipe.CanAfford(_clientInventory)) {
            Debug.Log($"Cannot afford {recipe.displayName} (client check).");
            HandleCraftFail(recipe, instantatiatedPopup, $"Cannot afford {recipe.displayName}");
            return;
        }
        // Craft the bitch, first remove items
        _clientInventory.ConsumeItems(recipe.requiredItems);
        // 2. Resources consumed. Now execute the recipe outcome.
        bool executionSuccess = recipe.ExecuteRecipe(context);
        if (executionSuccess) {
            HandleCraftSuccess(recipe);
        } else {
            HandleCraftFail(recipe, instantatiatedPopup, "Unable to craft!");
        }
    }

    private void HandleCraftSuccess(RecipeBaseSO recipe) {
        string name = recipe != null ? recipe.displayName : recipe.ID.ToString();
        Debug.Log($"UI: Successfully crafted {name}!");
    }

    private void HandleCraftFail(RecipeBaseSO recipe, UIPopup instantatiatedPopup, string reason) {
        string name = recipe != null ? recipe.displayName : recipe.ID.ToString();
        Debug.Log($"UI: Failed to craft {name}. Reason: {reason}");
        if (instantatiatedPopup == null) {
            Debug.LogError("Could not show a fail visual because we coudnt find an active popup!");
        }
        instantatiatedPopup.HandleFailVisual();
    }
}