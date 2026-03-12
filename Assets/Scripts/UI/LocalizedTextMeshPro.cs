using Assets.SimpleLocalization.Scripts;
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

    private void Localize() {
        if (LocalizationKey == string.Empty) return;
        string text = LocalizationManager.Localize(LocalizationKey);
        if(text == "") {
            Debug.LogWarning("Something went wrong trying to localize key " + LocalizationKey);
            return;
        }
        GetComponent<TextMeshProUGUI>().text = text;
    }
}
