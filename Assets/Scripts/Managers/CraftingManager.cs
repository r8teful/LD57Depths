
using UnityEngine;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Connection;

public class CraftingManager : NetworkBehaviour {
    public static CraftingManager Instance { get; private set; }
    // Events for client UI to hook into
    public static event System.Action<ushort> OnCraftSuccessClient;
    public static event System.Action<ushort, string> OnCraftFailClient;
    public override void OnStartServer() {
        base.OnStartServer();
        if (Instance == null) {
            Instance = this;
        } else {
            Debug.LogError("Multiple CraftingManager instances detected on server. Destroying this one.");
            Destroy(gameObject);
        }
    }

    public override void OnStartClient() {
        base.OnStartClient();
        if (Instance == null) {
            Instance = this; // Client also needs a reference for sending RPCs
        }
    }

    // Method for clients to call to request a craft
    public void ClientRequestCraft(ushort recipeId) {
        if (!base.IsClientInitialized)
            return; // Should only be called from a client

        // Send an RPC to the server.
        // The connection is automatically passed for ServerRpc if not specified.
        ServerProcessCraftRequest(recipeId);
        Debug.Log($"Client requested to craft recipe: {recipeId}");
    }


    [ServerRpc(RequireOwnership = false)] // RequireOwnership = false because this manager is a scene object
    private void ServerProcessCraftRequest(ushort recipeID, NetworkConnection crafterConnection = null) {
        Debug.Log($"Server received craft request for recipe: {recipeID} from client: {crafterConnection.ClientId}");
        var recipe = App.ResourceSystem.GetRecipeByID(recipeID);
        if (recipe == null) {
            Debug.LogWarning($"Client {crafterConnection.ClientId} tried to craft unknown recipe: {recipeID}");
            TargetCraftingFailed(crafterConnection, recipeID, "Recipe not found.");
            return;
        }

        // Get the player's NetworkObject and then their server-side inventory
        // This assumes player objects are owned by their connections
        if (crafterConnection.FirstObject == null ||
            !crafterConnection.FirstObject.TryGetComponent(out InventoryManager playerInventory)) {
            Debug.LogError($"Could not find ServerSidePlayerInventory for client {crafterConnection.ClientId}");
            TargetCraftingFailed(crafterConnection, recipeID, "Player inventory not found on server.");
            return;
        }

        // 1. Server-side validation: Does the player REALLY have the items?
        if (!playerInventory.ConsumeItems(recipe.requiredItems)) {
            Debug.LogWarning($"Client {crafterConnection.ClientId} failed to craft {recipeID}: Insufficient resources (server check).");
            TargetCraftingFailed(crafterConnection, recipeID, "Insufficient resources.");
            return;
        }

        // 2. Resources consumed. Now execute the recipe outcome.
        bool executionSuccess = recipe.ExecuteRecipe(crafterConnection, playerInventory);

        if (executionSuccess) {
            Debug.Log($"Client {crafterConnection.ClientId} successfully crafted {recipeID}.");
            TargetCraftingSucceeded(crafterConnection, recipeID);
            // The ServerSidePlayerInventory's SyncVars/SyncLists should handle item updates to the client.
            // If ExecuteRecipe does something else (e.g., grants an achievement, applies a buff),
            // you might need additional TargetRpcs from ExecuteRecipe or here.
        } else {
            Debug.LogError($"Client {crafterConnection.ClientId} crafting {recipeID}: Resources consumed, but recipe execution failed.");
            // CRITICAL: Decide how to handle this. Refund items? Log an error?
            // For now, we'll just notify failure. A robust system might try to refund.
            TargetCraftingFailed(crafterConnection, recipeID, "Recipe execution failed after consuming items.");
        }
    }

    [TargetRpc]
    private void TargetCraftingSucceeded(NetworkConnection conn, ushort recipeID) {
        Debug.Log($"Crafting Succeeded for recipe: {recipeID} on your client!");
        // Client-side: Play sound, show success UI, etc.
        // Your ClientCraftingUI can subscribe to an event triggered here.
        OnCraftSuccessClient?.Invoke(recipeID);
    }

    [TargetRpc]
    private void TargetCraftingFailed(NetworkConnection conn, ushort recipeID, string reason) {
        Debug.LogWarning($"Crafting Failed for recipe: {recipeID} on your client. Reason: {reason}");
        // Client-side: Show error message, play fail sound, etc.
        OnCraftFailClient?.Invoke(recipeID, reason);
    }


}