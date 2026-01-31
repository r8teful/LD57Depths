using UnityEngine;
using System;
using Sirenix.OdinInspector;

public class Interactable : MonoBehaviour, IInteractable {
    [Tooltip("The icon to display on the interaction popup.")]
    [SerializeField] private Sprite interactIcon;

    [Tooltip("The transform where the popup UI should appear. If null, this object's transform is used.")]
    [SerializeField] private Transform popupPosition;
    [ReadOnly]private bool isInteractionEnabled = true;

    [SerializeField] private bool _useOutline = true;
    [ShowIf("_useOutline")]
    [SerializeField] private SpriteRenderer _spriteRenderer; 

    public event Action<PlayerManager> OnInteract;
    [Tooltip("Action to execute when this object is no longer the closest interactable (e.g., to close an associated UI).")]
    public event Action OnCeaseInteractable;
    public event Action OnSetInteractable;

    public void DissableInteraction() => isInteractionEnabled = false;

    // --- Interface Implementation ---
    public Sprite InteractIcon => interactIcon;

    public bool CanInteract {
        get => isInteractionEnabled;
        set => isInteractionEnabled = value;
    }

    // --- Private State ---

    private CanvasInputWorld instantiatedPopup;
    private Material _outlineMat;

    void Awake() {
        // Default to this object's transform if no specific popup position is set.
        if (popupPosition == null) {
            popupPosition = transform;
        }
        if (_useOutline) {
            // create a new material instance
            if(_spriteRenderer == null) {
                Debug.LogError("Sprite renderer not assigned!");
                return;
            }
            _spriteRenderer.material = new(_spriteRenderer.material);
            _outlineMat = _spriteRenderer.material;
            _outlineMat.SetInt("_Enabled", 0);
        }
    }

    /// <summary>
    /// Called by an external manager (e.g., InputManager) when this becomes the
    /// closest interactable object.
    /// </summary>
    public void SetInteractable(bool isInteractable, Sprite interactPrompt = null) {
        if (isInteractable) {
            // Prevent creating duplicate popups
            if (instantiatedPopup == null) {
                instantiatedPopup = Instantiate(App.ResourceSystem.GetPrefab<CanvasInputWorld>("CanvasInputWorld"), popupPosition.position, Quaternion.identity, transform);
                instantiatedPopup.Init(this, interactPrompt);
                OnSetInteractable?.Invoke();
            }
            if (_useOutline) {
                _outlineMat.SetInt("_Enabled", 1);
            }
        } else {
            if (instantiatedPopup != null) {
                Destroy(instantiatedPopup.gameObject);
                instantiatedPopup = null; // Clear the reference
            }
            if (_useOutline) {
                _outlineMat.SetInt("_Enabled", 0);
            }
            // Invoke the event for when interaction ceases.
            OnCeaseInteractable?.Invoke();
        }
    }

    /// <summary>
    /// Called by the external manager when the interact button is pressed.
    /// </summary>
    public void Interact(PlayerManager player) {
        if (!CanInteract)
            return;
        // Invoke the main interaction event, passing along the client who interacted.
        OnInteract?.Invoke(player);
    }
}