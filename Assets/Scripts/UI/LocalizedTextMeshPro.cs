using Assets.SimpleLocalization.Scripts;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TextMeshProUGUI))]
public class LocalizedTextMeshPro : MonoBehaviour {
    public string LocalizationKey;
    public void OnEnable() {
        Localize();
        LocalizationManager.OnLocalizationChanged += Localize;
    }

    public void OnDisable() {
        LocalizationManager.OnLocalizationChanged -= Localize;
    }
    [Button]
    private void Localize() {
        if (string.IsNullOrEmpty(LocalizationKey)) return;
        if (InputPromptHandler.Instance == null) return;
        LocalizationManager.TryLocalize(LocalizationKey, out var s);
        var sFormated = InputPromptHandler.Instance.FormatWithIcons(s);
        string text = sFormated;
        if(text == "") {
            Debug.LogWarning("Something went wrong trying to localize key " + LocalizationKey);
            return;
        }
        GetComponent<TextMeshProUGUI>().text = text;
    }
}
