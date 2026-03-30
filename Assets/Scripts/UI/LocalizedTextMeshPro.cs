using Assets.SimpleLocalization.Scripts;
using InputSystemActionPrompts;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedTextMeshPro : MonoBehaviour {
    public string LocalizationKey;
    public bool _useSeparateGamepadKey;
    [ShowIf("_useSeparateGamepadKey")]
    public string LocalizationKeyController;
    private InputManager.DeviceType _currentDevice;

    public void OnEnable() {
        Localize();
        LocalizationManager.OnLocalizationChanged += Localize;
        InputManager.OnDeviceChanged += HandleDeviceChanged;
    }

    private void HandleDeviceChanged(InputManager.DeviceType type) {
        _currentDevice = type;
    }

    public void OnDisable() {
        LocalizationManager.OnLocalizationChanged -= Localize;
        InputManager.OnDeviceChanged -= HandleDeviceChanged;
    }
    [Button]
    private void Localize() {
        if (string.IsNullOrEmpty(LocalizationKey)) return;
        if (LocalizationKey == "UI.Upgrade.Buy") {
            Debug.Log("This");
        }
        
        string s;
        if(_useSeparateGamepadKey && _currentDevice == InputManager.DeviceType.Gamepad) {
            LocalizationManager.TryLocalize(LocalizationKeyController, out s);
        } else {
            LocalizationManager.TryLocalize(LocalizationKey, out s);
        }

        string text = InputDevicePromptSystem.InsertPromptSprites(s);
        if(text == "") {
            Debug.LogWarning("Something went wrong trying to localize key " + LocalizationKey);
            return;
        }
        GetComponent<TextMeshProUGUI>().text = text;
    }
}
