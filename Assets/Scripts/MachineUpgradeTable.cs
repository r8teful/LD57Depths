using System;
using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class MachineUpgradeTable : MonoBehaviour {
    private Interactable _interactable;
    [SerializeField] private UpgradeNodeSO _nodeToFix;
    [SerializeField] private Animator _animator;
    [SerializeField] private ParticleSystem _fixParticles;

    private void Awake() {
        _interactable = GetComponent<Interactable>();
        GameSetupManager.OnSetupComplete += MyAwake;
    }

    private void MyAwake() {
        if (SubmarineManager.Instance == null) {
            Debug.LogError("Can't find player!!");
        }
        SubmarineManager.Instance.OnSubUpgrade += UpgradePurchased;
    }

    private void UpgradePurchased(ushort id) {
        if(_nodeToFix.ID == id) {
            if (_animator != null) {
                _animator.Play("Fixed"); // todo add this
                _fixParticles.Play();
            }
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
        UIManager.Instance.UpgradePanelUIToggle();
    }

    public void CloseUpgradePanelUI() {
        UIManager.Instance.UpgradePanelUIClose();
    }
    internal void DEBUGToggle() {
        UIManager.Instance.UpgradePanelUIToggle();
    }
}