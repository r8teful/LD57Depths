using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

// Helper struct for required items
[System.Serializable]
public struct ItemQuantity {
    public ItemData item;
    public int quantity;
    public ItemQuantity(ItemData item, int q) {
        this.item = item;
        quantity = q;
    }
}
[System.Serializable]
public struct IDQuantity {
    public ushort itemID;
    public int quantity;
    public IDQuantity(ushort id, int q) {
        itemID = id;
        quantity = q;
    }
}
// Helper struct for UI status
public struct IngredientStatus {
    public ItemData Item { get; }
    public int RequiredAmount { get; }
    public int CurrentAmount { get; }
    public bool HasEnough => CurrentAmount >= RequiredAmount;

    public IngredientStatus(ItemData item, int requiredAmount, int currentAmount) {
        Item = item;
        RequiredAmount = requiredAmount;
        CurrentAmount = currentAmount;
    }
}
public struct NodeProgressionStatus {
    public int LevelMax { get; }
    public int LevelCurr { get; }
    public readonly bool ShouldShow => LevelMax > 1;
    public NodeProgressionStatus(int levelMax, int levelCurr) {
        LevelMax = levelMax;
        LevelCurr = levelCurr;
    }
}
// Usefull class for the actual result of a recipe. Will add more here later like research, upgrade etc..
public class RecipeExecutionContext {

    public readonly NetworkedPlayer Player;
    public readonly GameObject Source; // The originating object 

    public RecipeExecutionContext(NetworkedPlayer player, GameObject source = null) {
        Player = player;
        Source = source;
    }
    public static RecipeExecutionContext FromPlayer(NetworkedPlayer player) {
        return new RecipeExecutionContext(player);
    }
    public static RecipeExecutionContext FromObject(GameObject obj) {
        return new RecipeExecutionContext(null, obj);
    }
}
public abstract class RecipeBaseSO : ScriptableObject, IIdentifiable {

    [BoxGroup("Identification")]
    [HorizontalGroup("Identification/Left")]
    [VerticalGroup("Identification/Left/2")]
    public ushort RecipeID; // Unique ID for the recipe
    [VerticalGroup("Identification/Left/2")]
    public string displayName;
    [VerticalGroup("Identification/Left/2")]
    public string description;
    [VerticalGroup("Identification/Left/1")]
    [PreviewField(75), HideLabel, LabelWidth(0)]
    public Sprite icon;
    [BoxGroup("Gamepaly")]
    [VerticalGroup("Gamepaly/1")]
    public List<ItemQuantity> requiredItems = new List<ItemQuantity>();

    public ushort ID => RecipeID;

    /// <summary>
    /// Client side execution of the recipe.
    /// </summary>
    /// <returns>True if execution was successful, false otherwise.</returns>
    public abstract bool ExecuteRecipe(RecipeExecutionContext context);
   
    public virtual void PrepareRecipe(float value, List<ItemQuantity> resourcePool) {

    }
    /// <summary>
    /// Client-side check to see if the player has enough ingredients.
    /// This is primarily for UI feedback. The server will re-validate.
    /// </summary>
    public bool CanAfford(InventoryManager clientInventory) {
        if (clientInventory == null)
            return false;
        foreach (var req in requiredItems) {
            if (clientInventory.GetItemCount(req.item.ID) < req.quantity) {
                return false;
            }
        }
        return true;
    }
    /// <summary>
    /// Used to get approriate upgrades
    /// </summary>
    /// <returns></returns>
    public int GetRecipeValue() {
        int total = 0;
        foreach (var req in requiredItems) {
            total += (req.item.itemValue * req.quantity);
        }
        return total;
    }
    /// <summary>
    /// Gets the status of each ingredient for UI display.
    /// </summary>
    public List<IngredientStatus> GetIngredientStatuses(InventoryManager clientInventory) {
        var statuses = new List<IngredientStatus>();
        if (clientInventory == null) {
            // If no inventory, assume player has none of the items
            foreach (var req in requiredItems) {
                statuses.Add(new IngredientStatus(req.item, req.quantity, 0));
            }
            return statuses;
        }

        foreach (var req in requiredItems) {
            statuses.Add(new IngredientStatus(
                req.item,
                req.quantity,
                clientInventory.GetItemCount(req.item.ID)
            ));
        }
        return statuses;
    }
}