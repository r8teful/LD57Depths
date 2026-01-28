using System;
using System.Collections.Generic;
using UnityEngine;

// Has to hold upgrade info! 
public class UpgradeManagerPlayer : MonoBehaviour, IPlayerModule {

    private HashSet<ushort> unlockedUpgrades = new();
    public event Action<UpgradeRecipeSO> OnUpgradePurchased;
    private CraftingComponent _crafting;
    private PlayerManager _localNetworkedPlayer;

    public static UpgradeManagerPlayer LocalInstance { get; private set; }

    public HashSet<ushort> GetUnlockedUpgrades() => unlockedUpgrades;

    private void OnUpgradePurchase(UpgradeRecipeSO recipe) {
        Debug.Log($"Successfully purchased upgrade: {recipe.name}");
        OnUpgradePurchased?.Invoke(recipe);
    }

    public int InitializationOrder => 10;

    public void InitializeOnOwner(PlayerManager playerParent) {
        LocalInstance = this;
        _crafting = playerParent.CraftingComponent;
        _localNetworkedPlayer = playerParent;
    }
  
    public bool TryPurchaseUpgrade(UpgradeNodeSO node) {
        var tree = App.ResourceSystem.GetTreeByName(GameSetupManager.Instance.GetUpgradeTreeName());
        UpgradeRecipeSO recipe = node.GetUpgradeData(unlockedUpgrades, tree);
        // 1. Check if already purchased
        if (unlockedUpgrades.Contains(recipe.ID)) {
            Debug.LogWarning($"Attempted to purchase an already owned upgrade: {recipe.name}");
            return false;
        }

        // 2. Check prerequisites
        if (!node.ArePrerequisitesMet(unlockedUpgrades)) {
            return false;
        }
        ExecutionContext context = new(_localNetworkedPlayer);
        // 3. Try Execute recipe
        if (!_crafting.AttemptCraft(recipe,context)) {
            Debug.Log($"Failed to purchase {recipe.name}. Not enough currency.");
            return false;
        }

        // 4. Add upgrade to player's data
        unlockedUpgrades.Add(recipe.ID); // Will fire the OnChange event
        OnUpgradePurchase(recipe);
        return true;
    }

    // Rewards are unlocked without "trying" need this to track it so we know we have it unlocked
    public void AddUnlockedUpgrade(ushort ID) {
        unlockedUpgrades.Add(ID);
        var recipe = App.ResourceSystem.GetRecipeUpgradeByID(ID);
        OnUpgradePurchase(recipe);
    }
  


    internal bool IsUpgradePurchased(UpgradeRecipeSO upgradeData) {
        return unlockedUpgrades.Contains(upgradeData.ID);
    }
    /// <summary>
    /// Applies the one-time effects (UpgradeActions) of all upgrades currently owned by the player.
    /// This should be called once on game load.
    /// </summary>
    public void ApplyAllPurchasedUpgrades() {
        var allPurchased = GetUnlockedUpgrades();

        foreach (var recipe in allPurchased) {
            if (unlockedUpgrades.Contains(recipe)){
                var recipeData = App.ResourceSystem.GetRecipeUpgradeByID(recipe);
                recipeData.Execute(null); // BRUH how are we going to do this?
                // We can have the order depening on the ID, then the multiplication and addition will be done in the right order
            }
        }
    }

    internal void RemoveAllUpgrades() {
        unlockedUpgrades.Clear();
        _localNetworkedPlayer.UiManager.UpgradeScreen.UpgradeTreeInstance.UpdateNodeVisualData();
    }
}