﻿using System;
using System.Collections.Generic;
using UnityEngine;

// Has to hold upgrade info!
public class UpgradeManagerPlayer : Singleton<UpgradeManagerPlayer>, INetworkedPlayerModule {

    private Dictionary<ushort,UpgradeRecipeBase> unlockedUpgrades = new Dictionary<ushort, UpgradeRecipeBase>(); 
    public static event Action<UpgradeRecipeBase> OnUpgradePurchased;
    private CraftingComponent _crafting;

    public HashSet<ushort> GetUnlockedUpgrades() {
        HashSet<ushort> output = new HashSet<ushort>();
        foreach (var key in unlockedUpgrades) {
            output.Add(key.Key);
        }
        return output;
    }

    public int InitializationOrder => 10;

    public void InitializeOnOwner(NetworkedPlayer playerParent) {
        _crafting = playerParent.CraftingComponent;
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
            ToolController = NetworkedPlayer.LocalInstance.ToolController,
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
    /// <summary>
    /// Applies the one-time effects (UpgradeActions) of all upgrades currently owned by the player.
    /// This should be called once on game load.
    /// </summary>
    public void ApplyAllPurchasedUpgrades() {
        var allPurchased = GetUnlockedUpgrades();

        foreach (var recipe in allPurchased) {
            if(unlockedUpgrades.TryGetValue(recipe, out var recipeData)){
                recipeData.ExecuteRecipe(null); // BRUH how are we going to do this?
                // We can have the order depening on the ID, then the multiplication and addition will be done in the right order

            }
            
        }
    }
}

