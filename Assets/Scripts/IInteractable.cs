using FishNet.Object;
using FishNet.Object.Synchronizing;
using Unity.VisualScripting;
using UnityEngine;

public interface IInteractable {
    void Interact(NetworkObject client); // What happens when interacted with
    void SetInteractable(bool isInteractable, Sprite interactPrompt = null);
    public Sprite InteractIcon { get; }
    public bool CanInteract { get; set; }
}