using UnityEngine;
using UnityEngine.UI;

public class InputPromptIcon : MonoBehaviour {
    [SerializeField] private Image _interactIconImage;
    [SerializeField] private Image _interactPromptImage;

    public void Init(Sprite interactIcon, Sprite InteractPrompt) {
        if(interactIcon !=null)
            _interactIconImage.sprite = interactIcon;
        if(InteractPrompt != null)
            _interactPromptImage.sprite = InteractPrompt;
    }
}