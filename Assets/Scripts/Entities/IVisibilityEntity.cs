using UnityEngine;
using FishNet.Object; // Need NetworkObject reference

public enum VisibilityLayerType {
    Exterior, // Visible only when the local player is in the Exterior layer
    Interior  // Visible only when the local player is in a matching Interior layer
    // Could add AlwaysVisible, UILayer, etc. if needed later
}

public interface IVisibilityEntity {
    // Provides access to the underlying NetworkObject if needed by the manager
    NetworkObject NetworkObject { get; }

    // Declares which layer type this object belongs to
    VisibilityLayerType VisibilityScope { get; }

    // If VisibilityScope is Interior, this MUST return the relevant Interior ID.
    // Should return null or empty if VisibilityScope is Exterior.
    string AssociatedInteriorId { get; }

    // The core method the manager calls to toggle the object's visual/physical presence
    void SetObjectVisibility(bool isVisible);
}