using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputPromptHandler : PersistentSingleton<InputPromptHandler> {

    [Header("Input Actions")]
    [Tooltip("Your project's InputActionAsset.")]
    [SerializeField] private InputActionAsset actionAsset;


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

    public void NotifyRebindComplete() {
        OnBindingsChanged?.Invoke();
    }
    private string GetTMPRichText(string actionName) {
        InputAction action = actionAsset.FindAction(actionName, throwIfNotFound: false);
        var bindingPath = action.GetBindingDisplayString(InputBinding.DisplayStringOptions.DontOmitDevice);

        string iconName = GetIconName(bindingPath);
        return iconName != null ? $"<sprite name=\"{iconName}\">" : "";
    }


    /// <summary>
    /// Replaces [ActionName] tokens in a string with TMP sprite tags.
    /// "Press [Interact] to continue" -> "Press <sprite name="KeyboardE"> to continue"
    /// </summary>
    public string FormatWithIcons(string template) {
        return Regex.Replace(template, @"\[(\w+)\]", match => {
            string actionName = match.Groups[1].Value;
            return GetTMPRichText(actionName);
        });
    }

    private void HandleDeviceChanged(InputManager.DeviceType _) {
        // Device switched bindings may display differently  refresh all prompts
        OnBindingsChanged?.Invoke();
    }
    private string GetIconName(string bindingDisplayString) {

        int splitIndex = bindingDisplayString.LastIndexOf(" [");
        if (splitIndex == -1) {
            Debug.LogWarning($"[InputIcons] Unexpected display string format: '{bindingDisplayString}'");
            return null;
        }

        string control = bindingDisplayString[..splitIndex];// "B"
        string device = bindingDisplayString[(splitIndex + 2)..].TrimEnd(']');// "Xbox Controller"

        // Remove spaces so "Xbox Controller" -> "XboxController"
        device = device.Replace(" ", "");
        control = control.Replace(" ", "");

        return $"{device}{control}";
    }

}