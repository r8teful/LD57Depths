using System;
using UnityEngine;

// Links ingame to UI
[RequireComponent(typeof(Interactable))]
public class MachineControlPanel : MonoBehaviour {
    private Interactable _interactable;
    [SerializeField] private UpgradeNodeSO _nodeToInteract;
    [SerializeField] private ParticleSystem _brokenParticles;
    private void Awake() {
        _interactable = GetComponent<Interactable>(); 
        GameSetupManager.OnSetupComplete += MyAwake;

    }

    private void MyAwake() {
        if (PlayerManager.Instance == null) {
            Debug.LogError("Can't find player!!");
        }
        PlayerManager.Instance.UpgradeManager.OnUpgradePurchased += UpgradePurchased;
    }

    private void UpgradePurchased(UpgradeNodeSO sO) {
        if (sO == _nodeToInteract) {
            _interactable.CanInteract = true;
            _brokenParticles.Stop(true,ParticleSystemStopBehavior.StopEmitting);
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
    private void Start() {
        // Unless we've already unlocked it (save manager would need to tell us)
        _interactable.CanInteract = false;
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