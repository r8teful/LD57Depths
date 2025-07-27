// Has to hold upgrade info!
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using UnityEngine;

public class UpgradeManagerCommunal : NetworkBehaviour {

    private readonly SyncHashSet<ushort> unlockedCommunalUpgrades = new();
    public SyncHashSet<ushort> UnlockedCommunalUpgrades => unlockedCommunalUpgrades;
    public static UpgradeManagerCommunal Instance { get; private set; }
    private void Awake() {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }
    public bool ArePrerequisitesMet(UpgradeRecipeBase recipe) {
        if (recipe.prerequisite == null) {
            return true; // No prerequisites needed.
        }
        if (!unlockedCommunalUpgrades.Contains(recipe.prerequisite.ID)) {
            return false; // A prerequisite is missing.
        }
        return true;
    }

    public void PurchaseUpgrade(UpgradeRecipeBase recipe,CraftingComponent crafting) {
        // 1. Check if already purchased
        if (unlockedCommunalUpgrades.Contains(recipe.ID)) {
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
        if (!crafting.AttemptCraft(recipe, context)) {
            Debug.Log($"Failed to purchase {recipe.name}. Not enough currency.");
            return;
        }

        // 4. Add upgrade to player's data
        unlockedCommunalUpgrades.Add(recipe.ID);
        Debug.Log($"Successfully purchased upgrade: {recipe.name}");
    }

    internal float GetUpgradeValue(UpgradeType miningDamange) {
        throw new NotImplementedException();
    }

    [ServerRpc(RequireOwnership = false)]
    public void UnlockUpgradeRpc(ushort upgradeId) {
        // Check if the upgrade is already purchased
        if (unlockedCommunalUpgrades.Contains(upgradeId)) {
            Debug.LogWarning($"Communal upgrade {upgradeId} already purchased.");
            return;
        }
        // Add to communal upgrades
        unlockedCommunalUpgrades.Add(upgradeId);
    }
    internal bool IsUpgradePurchased(UpgradeRecipeBase upgradeData) {
        return unlockedCommunalUpgrades.Contains(upgradeData.ID);
    }

}