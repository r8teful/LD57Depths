using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputDeviceDetector : MonoBehaviour {
    public enum DeviceType { KeyboardMouse, Gamepad }
    public static event Action<DeviceType> OnDeviceChanged;
    public static DeviceType CurrentDevice { get; private set; } = DeviceType.KeyboardMouse;


    private void OnEnable() {
        InputSystem.onActionChange += HandleActionChange;
    }

    private void OnDisable() {
        InputSystem.onActionChange -= HandleActionChange;
    }
        
    /// <summary>
    /// Called by the Input System whenever an action is triggered.
    /// We check what device performed it and switch the active device type if needed.
    /// </summary>
    private void HandleActionChange(object obj, InputActionChange change) {
        // We only care about actions that were actually performed
        if (change != InputActionChange.ActionPerformed)
            return;

        if (obj is not InputAction action)
            return;

        var device = action.activeControl?.device;
        if (device == null)
            return;

        DeviceType detected = device is Gamepad ? DeviceType.Gamepad : DeviceType.KeyboardMouse;

        if (detected != CurrentDevice) {
            CurrentDevice = detected;
            OnDeviceChanged?.Invoke(CurrentDevice);
        }
    }
}