using UnityEngine;
using UnityEngine.UI;

public class InputPromptIcon : MonoBehaviour {
    [SerializeField] private Image _interactIconImage;
    [SerializeField] private Image _interactPromptImage;

    public void Init(Sprite interactIcon, Sprite IntreractPrompt) {
        _interactIconImage.sprite = interactIcon;
        _interactPromptImage.sprite = IntreractPrompt;
    }
}