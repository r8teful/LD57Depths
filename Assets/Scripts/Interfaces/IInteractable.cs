using FishNet.Object;
using UnityEngine;

public interface IInteractable {
    void Interact(NetworkedPlayer player); // What happens when interacted with
    void SetInteractable(bool isInteractable, Sprite interactPrompt = null);
    public Sprite InteractIcon { get; }
    public bool CanInteract { get; set; }
}