using TMPro;
using UnityEngine;

public class UIVersion : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI _versionText;

    private void Start() {
        _versionText.text = $"V{Application.version}";
    }
}
