using System;
using System.Collections.Generic;
using UnityEngine;

// Has to hold upgrade info!
public class UpgradeManager : StaticInstance<UpgradeManager> {

    private Dictionary<int,UpgradeRecipeBase> unlockedUpgrades = new Dictionary<int, UpgradeRecipeBase>(); 
    public static event Action<UpgradeRecipeBase> OnUpgradePurchased;
    private CraftingComponent _crafting;

    internal void Init(CraftingComponent crafting) {
        _crafting = crafting;
    }
    public bool ArePrerequisitesMet(UpgradeRecipeBase recipe) {
        if (recipe.prerequisite == null) {
            return true; // No prerequisites needed.
        }
        if (!unlockedUpgrades.ContainsKey(recipe.prerequisite.ID)) {
            return false; // A prerequisite is missing.
        }
        return true;
    }
    public void PurchaseUpgrade(UpgradeRecipeBase recipe) {
        // 1. Check if already purchased
        if (unlockedUpgrades.ContainsKey(recipe.ID)) {
            Debug.LogWarning($"Attempted to purchase an already owned upgrade: {recipe.name}");
            return;
        }

        // 2. Check prerequisites
        if (!ArePrerequisitesMet(recipe)) {
            return;
        }
        // Ugly but works for now
        RecipeExecutionContext context = new RecipeExecutionContext {
            ToolController = NetworkedPlayer.LocalInstance.ToolController
        };
        // 3. Try Execute recipe
        if (!_crafting.AttemptCraft(recipe,context)) {
            Debug.Log($"Failed to purchase {recipe.name}. Not enough currency.");
            return;
        }

        // 4. Add upgrade to player's data
        unlockedUpgrades.Add(recipe.ID,recipe);

        // 5. Fire the event to notify listeners
        Debug.Log($"Successfully purchased upgrade: {recipe.name}");
        OnUpgradePurchased?.Invoke(recipe);
    }

    internal float GetUpgradeValue(UpgradeType miningDamange) {
        throw new NotImplementedException();
    }


    internal bool IsUpgradePurchased(UpgradeRecipeBase upgradeData) {
        return unlockedUpgrades.ContainsKey(upgradeData.ID);
    }

}

