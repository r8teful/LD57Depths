using System;
using UnityEngine;
using UnityEngine.UI;

public class CanvasInputWorld : MonoBehaviour {
    private InputPromptIcon _instantiatedPrompt;
    public void Init(IInteractable interactable,Sprite interactPrompt) {
        _instantiatedPrompt = Instantiate(App.ResourceSystem.GetPrefab("InteractIndicator"),transform).GetComponent<InputPromptIcon>();
        _instantiatedPrompt.Init(interactable.InteractIcon, interactPrompt);
        GetComponent<Canvas>().sortingOrder = 99;
        GetComponent<Canvas>().sortingLayerName = "NoTilemapShadow";
    }
    

    internal void Destroy() {
        _instantiatedPrompt.Destroy(() => Destroy(gameObject));
    }

    // Positions the prompt to be in a different position so the player can press the button again and carry out the action
    internal void SetPromptNextStage(Transform parent) {
        var rect = _instantiatedPrompt.GetComponent<RectTransform>();
        _instantiatedPrompt.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        _instantiatedPrompt.transform.SetParent(parent);
        rect.pivot = new(1, -0.5f); // This is to get the position just right
        rect.anchorMin = new(1, 0);
        rect.anchorMax = new(1, 0);
        rect.anchoredPosition = new(0, 0);
    }
}