using UnityEngine;
using UnityEngine.UI;

public class InputPromptIcon : MonoBehaviour {
    [SerializeField] private Image _interactIconImage;
    [SerializeField] private Image _interactPromptImage;

    public void Init(Sprite interactIcon, Sprite InteractPrompt) {
        _interactIconImage.sprite = interactIcon;
        if (interactIcon == null)
            Destroy(_interactIconImage.gameObject);
        if(InteractPrompt != null)
            _interactPromptImage.sprite = InteractPrompt;
    }
}