using System;
using System.Collections.Generic;
using UnityEngine;

// Has to hold upgrade info!
public class UpgradeManager : StaticInstance<UpgradeManager> {

    private HashSet<int> unlockedUpgrades = new HashSet<int>(); // this could be usefull but maybe just the dictionary
    private Dictionary<int,UpgradeStatus> upgradeStatus = new Dictionary<int,UpgradeStatus>();
    private CraftingComponent _crafting;

    public static event Action<UpgradeRecipeSO> OnUpgradePurchased;
    internal void Init(CraftingComponent crafting) {
        _crafting = crafting;
    }
    public bool ArePrerequisitesMet(UpgradeRecipeSO recipe) {
        if (recipe.prerequisite == null) {
            return true; // No prerequisites needed.
        }
        if (!unlockedUpgrades.Contains(recipe.prerequisite.ID)) {
            return false; // A prerequisite is missing.
        }
        return true;
    }
    public void PurchaseUpgrade(UpgradeRecipeSO recipe) {
        // 1. Check if already purchased
        if (unlockedUpgrades.Contains(recipe.ID)) {
            Debug.LogWarning($"Attempted to purchase an already owned upgrade: {recipe.name}");
            return;
        }

        // 2. Check prerequisites
        if (!ArePrerequisitesMet(recipe)) {
            return;
        }
        
        // 3. Try Execute recipe
        if (!_crafting.AttemptCraft(recipe)) {
            Debug.Log($"Failed to purchase {recipe.name}. Not enough currency.");
            return;
        }

        // 4. Add upgrade to player's data
        unlockedUpgrades.Add(recipe.ID);

        // 5. Fire the event to notify listeners
        Debug.Log($"Successfully purchased upgrade: {recipe.name}");
        OnUpgradePurchased?.Invoke(recipe);
    }

    internal float GetUpgradeValue(UpgradeType miningDamange) {
        throw new NotImplementedException();
    }

   

    internal bool IsUpgradePurchased(UpgradeRecipeSO upgradeData) {
        return unlockedUpgrades.Contains(upgradeData.ID);
    }

    // Check if a node is unlocked
    private bool IsUnlocked(int id) {
        return unlockedUpgrades.Contains(id);
    }

}

