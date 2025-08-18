using FishNet.Connection;
using FishNet.Object;
using FishNet.Observing;
using UnityEngine;

// IDK we don't really need this right now, when we want to opimize, we either use something like this or a MathCondition
[CreateAssetMenu(menuName = "FishNet/Observer/Player State Condition", fileName = "New Player State Condition")]
public class LayerObserverCodition : ObserverCondition {
    public override ObserverConditionType GetConditionType() => ObserverConditionType.Normal;
    public override bool ConditionMet(NetworkConnection connection, bool currentlyAdded, out bool notProcessed) {
        notProcessed = false;
        if (!NetworkedPlayersManager.Instance.TryGetPlayer(NetworkObject.ClientManager.Connection.ClientId, out var player)) {
            return false;
        }
        if (!NetworkedPlayersManager.Instance.TryGetPlayer(connection.ClientId, out var playerOther)) {
            return false;
        }
        if (player.PlayerLayerController == null)
            return false;

        if (playerOther.PlayerLayerController == null)
            return false;
        // --- The Core Logic ---
        VisibilityLayerType playerLayer = player.PlayerLayerController.CurrentLayer.Value;
        VisibilityLayerType playerOtherLayer = playerOther.PlayerLayerController.CurrentLayer.Value;

        string playerInteriorID = player.PlayerLayerController.CurrentInteriorId.Value;
        string playerOtherInteriorID = playerOther.PlayerLayerController.CurrentInteriorId.Value;
        // Case 1: Observer is in the Exterior
        if (playerLayer == VisibilityLayerType.Exterior) {
            // Show the object ONLY if its scope is also Exterior.
            return playerOtherLayer == VisibilityLayerType.Exterior;
        }
        // Case 2: Observer is in an Interior
        else if (playerLayer == VisibilityLayerType.Interior) {
            // Show the object ONLY if its scope is Interior AND the Interior IDs match.
            return (playerOtherLayer == VisibilityLayerType.Interior && playerInteriorID == playerOtherInteriorID);
        }

        // Default to not showing the object if no condition is met.
        return false;
    }
}