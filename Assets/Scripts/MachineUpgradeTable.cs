using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class MachineUpgradeTable : MonoBehaviour {
    private Interactable _interactable;

    private void Awake() {
        _interactable = GetComponent<Interactable>();
    }

    private void OnEnable() {
        if (_interactable != null) {
            _interactable.OnInteract += HandleInteraction;
            _interactable.OnCeaseInteractable += CloseUpgradePanelUI;
        }
    }

    private void OnDisable() {
        if (_interactable != null) {
            _interactable.OnInteract -= HandleInteraction;
            _interactable.OnCeaseInteractable -= CloseUpgradePanelUI;
        }
    }

    private void HandleInteraction(PlayerManager interactor) {
        UIManager.Instance.UpgradePanelUIToggle();
    }

    public void CloseUpgradePanelUI() {
        UIManager.Instance.UpgradePanelUIClose();
    }
    internal void DEBUGToggle() {
        UIManager.Instance.UpgradePanelUIToggle();
    }
}