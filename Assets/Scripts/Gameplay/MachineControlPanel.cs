using FishNet.Object;
using UnityEngine;

// Links ingame to UI
[RequireComponent(typeof(Interactable))]
public class MachineControlPanel : MonoBehaviour {
    [SerializeField] private FixableEntity fixableEntity;
    private Interactable _interactable;

    private void Awake() {
        _interactable = GetComponent<Interactable>();

        if (fixableEntity == null) {
            fixableEntity = GetComponent<FixableEntity>();
        }
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

    private void HandleInteraction(NetworkedPlayer interactor) {
        if (fixableEntity != null && !fixableEntity.IsFixed) {
            Debug.Log("Machine is broken, cannot open control panel.");
            return;
        }

        UIManager.Instance.ControlPanelUIToggle();
    }

    public void CloseControlPanelUI() {
        UIManager.Instance.ControlPanelUIClose();
    }
    internal void DEBUGToggle() {
        UIManager.Instance.ControlPanelUIToggle();
    }  
}