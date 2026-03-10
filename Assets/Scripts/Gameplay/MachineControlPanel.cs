using UnityEngine;

// Links ingame to UI
[RequireComponent(typeof(Interactable))]
public class MachineControlPanel : MonoBehaviour {
    private Interactable _interactable;
    [SerializeField] private UpgradeNodeSO _nodeToInteract;
    [SerializeField] private ParticleSystem _brokenParticles;
    [SerializeField] private ParticleSystem _fixParticles;
    [SerializeField] private Animator _animatorMachine;

    private void Awake() {
        _interactable = GetComponent<Interactable>(); 
        GameManager.OnSetupComplete += MyAwake;
    }

    private void MyAwake() {
        if (SubmarineManager.Instance == null) {
            Debug.LogError("Can't find player!!");
        }
        SubmarineManager.Instance.OnSubUpgrade += UpgradePurchased;
    }

    private void UpgradePurchased(ushort ID) {
        if (ID == _nodeToInteract.ID) {
            _interactable.CanInteract = true;
            _brokenParticles.Stop(true,ParticleSystemStopBehavior.StopEmitting);
            _animatorMachine.Play("Fixed");
            _fixParticles.Play();
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