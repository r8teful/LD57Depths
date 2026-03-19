using UnityEngine;
using UnityEngine.UI;

namespace Assets.SimpleLocalization.Scripts
{
	/// <summary>
	/// Localize dropdown component.
	/// </summary>
    [RequireComponent(typeof(Dropdown))]
    public class LocalizedDropdown : MonoBehaviour
    {
        public string[] LocalizationKeys;

        public void Start()
        {
            Localize();
            LocalizationManager.OnLocalizationChanged += Localize;
        }

        public void OnDestroy()
        {
            LocalizationManager.OnLocalizationChanged -= Localize;
        }

        private void Localize()
        {
	        var dropdown = GetComponent<Dropdown>();

			for (var i = 0; i < LocalizationKeys.Length; i++)
	        {
                LocalizationManager.TryLocalize(LocalizationKeys[i], out var s);
                dropdown.options[i].text = s;
	        }

	        if (dropdown.value < LocalizationKeys.Length)
	        {
                LocalizationManager.TryLocalize(LocalizationKeys[dropdown.value], out var s);
                dropdown.captionText.text = s;
	        }
        }
    }
}