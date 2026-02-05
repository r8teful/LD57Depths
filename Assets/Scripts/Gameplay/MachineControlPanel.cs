using UnityEngine;

// Links ingame to UI
[RequireComponent(typeof(Interactable))]
public class MachineControlPanel : MonoBehaviour {
    private Interactable _interactable;
    private void Awake() {
        _interactable = GetComponent<Interactable>();
    }
    private void OnEnable() {
        if (_interactable != null) {
            _interactable.OnInteract += HandleInteraction;
            _interactable.OnCeaseInteractable += CloseControlPanelUI;
        }
    }

    private void OnDisable() {
        if (_interactable != null) {
            _interactable.OnInteract -= HandleInteraction;
            _interactable.OnCeaseInteractable -= CloseControlPanelUI;
        }
    }

    private void HandleInteraction(PlayerManager interactor) {
        UIManager.Instance.ControlPanelUIToggle();
    }

    public void CloseControlPanelUI() {
        UIManager.Instance.ControlPanelUIClose();
    }
    internal void DEBUGToggle() {
        UIManager.Instance.ControlPanelUIToggle();
    }  
}