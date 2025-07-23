using FishNet.Object;
using FishNet.Object.Synchronizing;
using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class FixableEntity : NetworkBehaviour, IInteractable, IPopupInfo {
    [SerializeField] private Sprite FixIcon;
    [SerializeField] private Transform _popupPos;
    private CanvasInputWorld instantatiatedCanvas;
    private UIPopup instantatiatedPopup;
    private SubInterior _subParent;
    public RecipeBaseSO fixRecipe;
    public event Action<IPopupInfo, bool> OnPopupShow; // This IPopupInfo is different because we instantiate the popup directly
    public event Action PopupDataChanged;
    public SpriteRenderer SpriteRenderer;
    public Sprite InteractIcon => FixIcon;

    private readonly SyncVar<bool> _isFixed = new SyncVar<bool>(false);
    private readonly SyncVar<int> _interactingClient = new SyncVar<int>(-1);
    public SyncVar<bool> IsFixed => _isFixed;
    public SyncVar<int> InteractingClient => _interactingClient;


    bool IInteractable.CanInteract {
        get {
            return _canInteract;
        }

        set {
            _canInteract = value;
        }
    }

    private bool _canInteract = true;
    private void Start() {
        // Create a copy of the material
        SpriteRenderer.material = new Material(SpriteRenderer.material);
    }
    public void InitParent(SubInterior subParent) {
        // This is now obviously tied to the sub interior only. All we really need is some sort of manager that handles the state of this object
        // So we can call manager.ThisObjectIsFixed and then the manager will handle the rest
        // -- Update -- So this object now is even more tied to the sub interior, because, again, it needs some kind of manager that handles the execution of the fixing
        //SetIsBrokenBool(isBroke);
        _subParent = subParent;
    }
    public void SetMaterialBrokenBool(bool isBroken) {
        SpriteRenderer.material.SetInt("_Damaged", isBroken ? 1 : 0);
        if (!isBroken) {
            Destroy(this); // This makes sence right?
        }
    }
    [ServerRpc]
    private void SetFixedRpc() {
        _isFixed.Value = true;
    }
    
    public void SetFixed() {
        _subParent.EntityFixed(this);
        SetMaterialBrokenBool(false);
        SetFixedRpc();
        // TODO other functionality etc etc...
    }

    public void SetInteractable(bool isInteractable, Sprite interactPrompt = null) {
        if (isInteractable) {
            instantatiatedCanvas = Instantiate(App.ResourceSystem.GetPrefab("CanvasInputWorld"), _popupPos.position,Quaternion.identity,transform).GetComponent<CanvasInputWorld>();
            instantatiatedCanvas.Init(this, interactPrompt);
        } else {
            if (instantatiatedCanvas != null) {
                Destroy(instantatiatedCanvas.gameObject);
            }
        }
    }

    // The best would be to use the already existing popup manager to setup the thing but I don't know what the real benefits are atm, this works for now
    public void Interact(NetworkObject client) {
        if(instantatiatedCanvas != null && instantatiatedPopup == null) {
            if (_isFixed) {
                // Open the UI for this object?
                return; 
            }
            var clientInventory = client.GetComponent<NetworkedPlayerInventory>().GetInventoryManager(); // Probably really bad to do this but EH?
            instantatiatedPopup = Instantiate(App.ResourceSystem.GetPrefab("PopupWorld"), instantatiatedCanvas.transform).GetComponent<UIPopup>();
            instantatiatedPopup.SetData(new(fixRecipe.displayName, fixRecipe.description, fixRecipe.GetIngredientStatuses(clientInventory)));
            instantatiatedCanvas.SetPromptNextStage(instantatiatedPopup.transform);
        } else if(instantatiatedPopup != null) {
            // Basically pressing again while the popup is already open
            // TODO use PopupManager.CurrentPopup!!
            // Passing the instantiated popup so we can show visual feedback BTW, this should probably be handled by PopupManager, it already has a CurrentPopup variable
            var context = new RecipeExecutionContext { Entity = this, NetworkedPlayer = client.GetComponent<NetworkedPlayer>()};
            _subParent.TryFixEntity(fixRecipe, instantatiatedPopup, context);
        }
        //PopupManager.Instance.TryShowWorldPopup(this,client);
    }

    public PopupData GetPopupData(InventoryManager clientInv) {
        return new(fixRecipe.name, fixRecipe.description, fixRecipe.GetIngredientStatuses(clientInv));
    }
}