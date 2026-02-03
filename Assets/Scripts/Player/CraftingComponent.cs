using UnityEngine;

// Just on the player for now because fuck it
public class CraftingComponent : MonoBehaviour, IPlayerModule {
    private InventoryManager _inventory; // Your existing client inventory manager

    public int InitializationOrder => 1;

    public void InitializeOnOwner(PlayerManager playerParent) {
        _inventory = SubmarineManager.Instance.SubInventory;
    }
    // Not that SubmarineManager handles the subupgrading stuff
    public bool AttemptCraft(RecipeBaseSO recipe, ExecutionContext context = null, UIPopup instantatiatedPopup = null) {
        // TODO possible use client inventoy from context here. But no, we don't really want to change that, or have other scripts store it, just have it be stored here and create a new context each time
        // We call ExecuteRecipe
        if (recipe == null || _inventory == null) {
            Debug.LogWarning("recipe or Inventory null!");
            return false;
        }

        if (instantatiatedPopup == null) {
            // Just take the current popup from the popupManager
            instantatiatedPopup = PopupManager.Instance.CurrentPopup;
        }
        // Client-side check, WE ARE NOT CHECKING ON SERVER NOW!!
        if (!recipe.CanAfford(_inventory)) {
            Debug.Log($"Cannot afford {recipe.displayName} (client check).");
            HandleCraftFail(recipe, instantatiatedPopup, $"Cannot afford {recipe.displayName}");
            return false;
        }
        // Craft the bitch
        recipe.Execute(context);
        _inventory.RemoveItems(recipe.requiredItems); // Only consume when recipe success
        return true;
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