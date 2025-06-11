using UnityEngine;
using UnityEngine.UI;

public class CanvasInputWorld : MonoBehaviour {
    private InputPromptIcon _instantiatedPrompt;
    public void Init(IInteractable interactable,Sprite interactPrompt) {
        _instantiatedPrompt = Instantiate(App.ResourceSystem.GetPrefab("InteractIndicator"),transform).GetComponent<InputPromptIcon>();
        _instantiatedPrompt.Init(interactable.InteractIcon, interactPrompt);
    }
    // Don't think we actually have to do this because it is a child but eh I've written it now
    public void OnDestroy() {
        if (_instantiatedPrompt != null) {
            Destroy(_instantiatedPrompt.gameObject );
        }
    }

    // Positions the prompt to be in a different position so the player can press the button again and carry out the action
    internal void SetPromptNextStage(Transform parent) {
        var rect = _instantiatedPrompt.GetComponent<RectTransform>();
        _instantiatedPrompt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        _instantiatedPrompt.transform.SetParent(parent);
        rect.pivot = new(1, 0.25f); // This is to get the position just right
        rect.anchorMin = new(1, 0);
        rect.anchorMax = new(1, 0);
    }
}