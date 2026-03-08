using UnityEngine;

public interface IInteractable {
    void Interact(PlayerManager player); // What happens when interacted with
    void SetInteractable(bool isInteractable, Sprite interactPrompt = null);
    public Sprite InteractIcon { get; }
    public bool CanInteract { get; set; }
    float InteractionRangeOverride { get; } // -1, don't override
}