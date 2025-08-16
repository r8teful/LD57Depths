using NUnit;
using System.Collections;
using UnityEngine;

// Just on the player for now because fuck it
public class CraftingComponent : MonoBehaviour, INetworkedPlayerModule {
    private InventoryManager _clientInventory; // Your existing client inventory manager

    public int InitializationOrder => 1;

    public void InitializeOnOwner(NetworkedPlayer playerParent) {
        _clientInventory = playerParent.InventoryN.GetInventoryManager();
    }
    public bool AttemptCraft(RecipeBaseSO recipe, RecipeExecutionContext context = null, UIPopup instantatiatedPopup = null) {
        // TODO possible use client inventoy from context here. But no, we don't really want to change that, or have other scripts store it, just have it be stored here and create a new context each time
        // We call ExecuteRecipe
        if (recipe == null || _clientInventory == null) {
            Debug.LogWarning("recipe or Inventory null!");
            return false;
        }
        if (context == null) {
            // Popuplate it with inv
            context = new RecipeExecutionContext { PlayerInventory = _clientInventory };
        } else if (context.PlayerInventory == null) {
            // If context was passed but PlayerInventory is null, set it
            context.PlayerInventory = _clientInventory;
        }
        if (instantatiatedPopup == null) {
            // Just take the current popup from the popupManager
            instantatiatedPopup = PopupManager.Instance.CurrentPopup;
        }
        // Client-side check, WE ARE NOT CHECKING ON SERVER NOW!!
        if (!recipe.CanAfford(_clientInventory)) {
            Debug.Log($"Cannot afford {recipe.displayName} (client check).");
            HandleCraftFail(recipe, instantatiatedPopup, $"Cannot afford {recipe.displayName}");
            return false;
        }
        // Craft the bitch
        bool executionSuccess = recipe.ExecuteRecipe(context);
        if (executionSuccess) {
            HandleCraftSuccess(recipe);
            _clientInventory.ConsumeItems(recipe.requiredItems); // Only consume when recipe success
        } else {
            HandleCraftFail(recipe, instantatiatedPopup, "Unable to craft!");
            return false;
        }
        return true;
    }
    public void StartAttemptCraftRoutine(RecipeBaseSO recipe, RecipeExecutionContext context = null, UIPopup instantatiatedPopup = null) {
        StartCoroutine(AttemptCraftRoutine(recipe, context, instantatiatedPopup));
    }

    private IEnumerator AttemptCraftRoutine(RecipeBaseSO recipe, RecipeExecutionContext context = null, UIPopup instantatiatedPopup = null) {
        // Validate inputs
        if (recipe == null || _clientInventory == null) {
            Debug.LogWarning("recipe or Inventory null!");
            yield break; // Exit the coroutine early
        }

        // Set up context if not provided or incomplete
        if (context == null) {
            context = new RecipeExecutionContext { PlayerInventory = _clientInventory };
        } else if (context.PlayerInventory == null) {
            context.PlayerInventory = _clientInventory;
        }

        // Set up popup if not provided
        if (instantatiatedPopup == null) {
            instantatiatedPopup = PopupManager.Instance.CurrentPopup;
        }

        // Client-side affordability check
        if (!recipe.CanAfford(_clientInventory)) {
            Debug.Log($"Cannot afford {recipe.displayName} (client check).");
            HandleCraftFail(recipe, instantatiatedPopup, $"Cannot afford {recipe.displayName}");
            yield break; // Exit the coroutine early
        }

        // Execute the recipe asynchronously and wait for it to complete
        yield return recipe.ExecuteRecipeRoutine(context);

        // Check the result of the execution (assumes context.Success is set by ExecuteRecipe)
        if (context.Success) {
            _clientInventory.ConsumeItems(recipe.requiredItems); // Consume items on success
            HandleCraftSuccess(recipe); // Handle success scenario
        } else {
            HandleCraftFail(recipe, instantatiatedPopup, "Unable to craft!"); // Handle failure scenario
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