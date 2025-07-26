using UnityEngine;
using System.Collections.Generic;

public class InteriorManager : Singleton<InteriorManager> {

    private Dictionary<string, InteriorInstance> _registeredInteriors = new Dictionary<string, InteriorInstance>();

    public void RegisterInterior(InteriorInstance interior) {
        if (interior == null || string.IsNullOrEmpty(interior.InteriorId)) return;

        if (_registeredInteriors.ContainsKey(interior.InteriorId)) {
            Debug.LogWarning($"Interior Manager: An interior with ID '{interior.InteriorId}' is already registered. Overwriting reference. Object: {interior.gameObject.name}");
            _registeredInteriors[interior.InteriorId] = interior; // Overwrite or update
        } else {
            _registeredInteriors.Add(interior.InteriorId, interior);
           // Debug.Log($"Interior Manager: Registered '{interior.InteriorId}'.");
        }
    }

    public void UnregisterInterior(InteriorInstance interior) {
        if (interior == null || string.IsNullOrEmpty(interior.InteriorId)) return;

        if (_registeredInteriors.ContainsKey(interior.InteriorId) && _registeredInteriors[interior.InteriorId] == interior) {
            _registeredInteriors.Remove(interior.InteriorId);
            Debug.Log($"Interior Manager: Unregistered '{interior.InteriorId}'.");
        }
    }

    public InteriorInstance GetInteriorById(string id) {
        _registeredInteriors.TryGetValue(id, out InteriorInstance interior);
        return interior; // Returns null if not found
    }

    public void ActivateInterior(string interiorId) {
        bool foundActive = false;
        foreach (var kvp in _registeredInteriors) {
            bool shouldBeActive = kvp.Key == interiorId;
            if (kvp.Value != null) {
                if (shouldBeActive) {
                    kvp.Value.PositionToAnchor();
                    kvp.Value.SetInteriorActive(true); // Now activate components
                    foundActive = true;
                    // Debug.Log($"Activated and Positioned Interior: {kvp.Key}");
                } else {
                    kvp.Value.SetInteriorActive(false); // Deactivate others
                                                        // Optional: Reset position when deactivated fully?
                                                        // kvp.Value.ResetPosition();
                }
            }
        }
        if (!string.IsNullOrEmpty(interiorId) && !foundActive) {
            Debug.LogWarning($"ActivateInterior: Could not find registered interior with ID '{interiorId}' to activate.");
        }
    }

    // Method to deactivate ALL interiors
    public void DeactivateAllInteriors() {
        foreach (var kvp in _registeredInteriors) {
            if (kvp.Value != null) {
                kvp.Value.SetInteriorActive(false);
            }
        }
        // Debug.Log("Deactivated all interiors.");
    }
}