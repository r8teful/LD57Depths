using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using System.Collections.Generic;
using UnityEngine;

// Has to hold upgrade info! Specific to THIS player 
public class UpgradeManagerPlayer : NetworkBehaviour, INetworkedPlayerModule {

    //private Dictionary<ushort,UpgradeRecipeSO> unlockedUpgrades = new Dictionary<ushort, UpgradeRecipeSO>();
    private readonly SyncHashSet<ushort> unlockedUpgrades = new();
    public event Action<UpgradeRecipeSO> OnUpgradePurchased;
    private CraftingComponent _crafting;
    private NetworkedPlayer _localNetworkedPlayer;

    public static UpgradeManagerPlayer LocalInstance { get; private set; }

    public HashSet<ushort> GetUnlockedUpgrades() {
        //HashSet<ushort> output = new HashSet<ushort>();
        //foreach (var key in unlockedUpgrades.GetCollection(false)) {
        //    output.Add(key);
        //}
        //return output;
        return unlockedUpgrades.GetCollection(false);
    }
    public override void OnStartClient() {
        base.OnStartClient();
        unlockedUpgrades.OnChange += OnUpgradeChange;
    }

    private void OnUpgradeChange(SyncHashSetOperation op, ushort item, bool asServer) {
        if (asServer)
            return; 
        if (op == SyncHashSetOperation.Add) {
            // Now we fire the event to notify listeners, this will work for multiplayer now
            var recipe = App.ResourceSystem.GetRecipeUpgradeByID(item);
            Debug.Log($"Successfully purchased upgrade: {recipe.name}");
            OnUpgradePurchased?.Invoke(recipe);
        }
    }

    public int InitializationOrder => 10;

    public void InitializeOnOwner(NetworkedPlayer playerParent) {
        LocalInstance = this;
        _crafting = playerParent.CraftingComponent;
        _localNetworkedPlayer = playerParent;
    }
    public override void OnStopClient() {
        base.OnStopClient();
        LocalInstance = null;
    }
  
    // this is bad to do because the UpgradePurchased event will only be called on the local clients
    public bool TryPurchaseUpgrade(UpgradeNodeSO node) {
        var tree = App.ResourceSystem.GetTreeByName(GameSetupManager.LocalInstance.GetUpgradeTreeName());
        UpgradeRecipeSO recipe = node.GetNextUpgradeForNode(unlockedUpgrades.Collection, tree);
        // 1. Check if already purchased
        if (unlockedUpgrades.Contains(recipe.ID)) {
            Debug.LogWarning($"Attempted to purchase an already owned upgrade: {recipe.name}");
            return false;
        }

        // 2. Check prerequisites
        if (!node.ArePrerequisitesMet(unlockedUpgrades.Collection)) {
            return false;
        }
        var context = RecipeExecutionContext.FromPlayer(_localNetworkedPlayer);
        // 3. Try Execute recipe
        if (!_crafting.AttemptCraft(recipe,context)) {
            Debug.Log($"Failed to purchase {recipe.name}. Not enough currency.");
            return false;
        }

        // 4. Add upgrade to player's data
        unlockedUpgrades.Add(recipe.ID);
        return true;
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
                recipeData.ExecuteRecipe(null); // BRUH how are we going to do this?
                // We can have the order depening on the ID, then the multiplication and addition will be done in the right order

            }
            
        }
    }
}