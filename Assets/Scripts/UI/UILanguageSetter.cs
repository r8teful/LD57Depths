using Assets.SimpleLocalization.Scripts;
using UnityEngine;
using UnityEngine.UI;

public class UILanguageSetter : MonoBehaviour{
    public Button LanguageButton;
    public Transform ButtonContainer;

    void Start() {
        foreach(var language in LocalizationManager.Dictionary) {
            var button = Instantiate(LanguageButton, ButtonContainer);
            button.onClick.AddListener(() => OnLanguageClicked(language.Key));
            var bv = button.GetComponentInChildren<ButtonMenuVisual>();
            bv.SetText(language.Key);
        }
    }
    public void OnLanguageClicked(string language) {
        Debug.Log("On language changed to " + language);
    }

}
