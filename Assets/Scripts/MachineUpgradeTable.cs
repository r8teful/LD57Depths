using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class MachineUpgradeTable : MonoBehaviour {
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
        if (fixableEntity != null && !fixableEntity.IsFixed) {
            Debug.Log("Machine is broken, cannot open control panel.");
            return;
        }

        UIManager.Instance.UpgradePanelUIToggle();
    }

    public void CloseUpgradePanelUI() {
        UIManager.Instance.UpgradePanelUIClose();
    }
    internal void DEBUGToggle() {
        UIManager.Instance.UpgradePanelUIToggle();
    }
}