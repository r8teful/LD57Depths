using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class MachineControlPanel : NetworkBehaviour, IInteractable {
    [SerializeField] private Sprite interactIcon;
    private CanvasInputWorld instantiatedWorldCanvas;
    private GameObject instantiatedUI;
    [SerializeField] private Transform _popupPos;
    public Sprite InteractIcon => interactIcon;
    private bool _canInteract = true;
    public bool CanInteract {
        get {
            return _canInteract && IsFixed();
        }

        set {
            _canInteract = value;
        }
    }
  
    private readonly SyncVar<int> _interactingClient = new SyncVar<int>(-1);
    public SyncVar<int> InteractingClient => _interactingClient;

    public void Interact(NetworkObject client) {
        if (!CanInteract)
            return;
        if (instantiatedWorldCanvas != null) {
            if(instantiatedUI == null) {
                OpenUI();
            } else {
                CloseUI();
            }
        }
    }
    private void OpenUI() {
        instantiatedUI = Instantiate(App.ResourceSystem.GetPrefab("ControlPanelUICanvas"));
    }
    private void CloseUI() {
        if(instantiatedUI != null) {
            Destroy(instantiatedUI);
        }
    }
    // This is now just a direct copy of FixableEntity
    public void SetInteractable(bool isInteractable, Sprite interactPrompt = null) {
        if (isInteractable) {
            instantiatedWorldCanvas = Instantiate(App.ResourceSystem.GetPrefab("CanvasInputWorld"), _popupPos.position, Quaternion.identity, transform).GetComponent<CanvasInputWorld>();
            instantiatedWorldCanvas.Init(this, interactPrompt);
        } else {
            if (instantiatedWorldCanvas != null) {
                Destroy(instantiatedWorldCanvas.gameObject);
            }
            CloseUI();
        }
    }
    private bool IsFixed() {
        if (gameObject.TryGetComponent<FixableEntity>(out var fixEnt)) {
            return fixEnt.IsFixed;
        } else {
            return false;
        }
    }
}