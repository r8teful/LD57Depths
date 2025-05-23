using FishNet.Connection;
using UnityEngine;

public abstract class CraftingRecipeSO : ScriptableObject {
    public abstract void ExecuteRecipe(NetworkConnection player);
}