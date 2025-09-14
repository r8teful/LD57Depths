using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Has to hold upgrade info!
public class UpgradeManagerPlayer : Singleton<UpgradeManagerPlayer>, INetworkedPlayerModule {

    private Dictionary<ushort,UpgradeRecipeSO> unlockedUpgrades = new Dictionary<ushort, UpgradeRecipeSO>(); 
    public static event Action<UpgradeRecipeSO> OnUpgradePurchased;
    private CraftingComponent _crafting;
    private NetworkedPlayer _localNetworkedPlayer;

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
        _localNetworkedPlayer = playerParent;
    }
    public bool ArePrerequisitesMet(UpgradeRecipeSO recipe) {
        var p = recipe.GetPrerequisites();
        if (p == null || p.Count == 0) {
            return true; // No prerequisites needed.
        }

        // true if AT LEAST ONE prerequisite is unlocked.
        if (p.Any(u => unlockedUpgrades.ContainsKey(u.ID))) {
            return true;
        }
        return false;
    }
    public void TryPurchaseUpgrade(UpgradeRecipeSO recipe) {
        // 1. Check if already purchased
        if (unlockedUpgrades.ContainsKey(recipe.ID)) {
            Debug.LogWarning($"Attempted to purchase an already owned upgrade: {recipe.name}");
            return;
        }

        // 2. Check prerequisites
        if (!ArePrerequisitesMet(recipe)) {
            return;
        }
        var context = RecipeExecutionContext.FromPlayer(_localNetworkedPlayer);
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

  


    internal bool IsUpgradePurchased(UpgradeRecipeSO upgradeData) {
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

