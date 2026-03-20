using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Central service that resolves an action name to a TMP sprite tag based on
/// the currently active device and the player's current bindings.

/// Add this component once to a persistent GameObject alongside InputDeviceDetector.
/// </summary>
public class InputPromptHandler : PersistentSingleton<InputPromptHandler> {

    [Header("Input Actions")]
    [Tooltip("Your project's InputActionAsset.")]
    [SerializeField] private InputActionAsset actionAsset;

    [Header("Icon Databases")]
    [Tooltip("Database for Keyboard + Mouse bindings.")]
    [SerializeField] private TextIconsDatabase keyboardMouseDatabase;

    [Tooltip("Database for Gamepad bindings.")]
    [SerializeField] private TextIconsDatabase gamepadDatabase;

    [Header("Binding Override Map (optional)")]
    [Tooltip("Name of the control scheme used for binding overrides. Leave empty to use the first matching binding.")]
    [SerializeField] private string controlSchemeName = "";
 
    /// <summary>
    /// Fired whenever bindings change (rebind) or the active device type switches.
    /// All InputPromptText components listen to this to refresh their display text.
    /// </summary>
    public static event Action OnBindingsChanged;


    private void OnEnable() {
        InputManager.OnDeviceChanged += HandleDeviceChanged;
    }

    private void OnDisable() {
        InputManager.OnDeviceChanged -= HandleDeviceChanged;
    }


    public string GetPromptTag(string actionName) {
        if (actionAsset == null) {
            Debug.LogWarning("[InputPromptService] No InputActionAsset assigned.");
            return FallbackTag();
        }

        InputAction action = actionAsset.FindAction(actionName, throwIfNotFound: false);
        if (action == null) {
            Debug.LogWarning($"[InputPromptService] Action '{actionName}' not found in asset.");
            return FallbackTag();
        }

        string bindingPath = GetEffectiveBindingPath(action);
        if (string.IsNullOrEmpty(bindingPath))
            return FallbackTag();

        TextIconsDatabase db = GetDatabaseForCurrentDevice();
        return db != null ? db.GetSpriteTag(bindingPath) : FallbackTag();
    }

    /// <summary>
    /// Call this after completing a rebind operation so all prompt texts refresh.
    /// </summary>
    public void NotifyRebindComplete() {
        OnBindingsChanged?.Invoke();
    }

    private void HandleDeviceChanged(InputManager.DeviceType _) {
        // Device switched → bindings may display differently → refresh all prompts
        OnBindingsChanged?.Invoke();
    }

    /// <summary>
    /// Finds the effective binding path for the current device type.
    /// Prefers override bindings (from rebinding) over the original path.
    /// </summary>
    private string GetEffectiveBindingPath(InputAction action) {
        bool wantGamepad = InputManager.CurrentDevice == InputManager.DeviceType.Gamepad;

        for (int i = 0; i < action.bindings.Count; i++) {
            InputBinding binding = action.bindings[i];

            // Skip composite parts — we want the composite entry itself or simple bindings
            if (binding.isPartOfComposite)
                continue;

            bool isGamepadBinding = binding.path.StartsWith("<Gamepad>", StringComparison.OrdinalIgnoreCase)
                                 || binding.path.StartsWith("<DualShock>", StringComparison.OrdinalIgnoreCase)
                                 || binding.path.StartsWith("<XInputController>", StringComparison.OrdinalIgnoreCase);

            if (wantGamepad != isGamepadBinding)
                continue;

            // If a control scheme filter is set, check group match
            if (!string.IsNullOrEmpty(controlSchemeName) &&
                !binding.groups.Contains(controlSchemeName, StringComparison.OrdinalIgnoreCase))
                continue;

            // Use override path if present (player has rebound this action), else the original
            string path = !string.IsNullOrEmpty(binding.overridePath)
                ? binding.overridePath
                : binding.effectivePath;

            if (!string.IsNullOrEmpty(path))
                return path;
        }

        return string.Empty;
    }

    private TextIconsDatabase GetDatabaseForCurrentDevice() {
        return InputManager.CurrentDevice == InputManager.DeviceType.Gamepad
            ? gamepadDatabase
            : keyboardMouseDatabase;
    }

    private static string FallbackTag() => "<sprite name=\"unknown_key\">";
}
