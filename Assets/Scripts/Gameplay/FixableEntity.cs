using System;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class FixableEntity : MonoBehaviour, IInteractable, IPopupInfo {
    [SerializeField] private Sprite FixIcon;
    [SerializeField] private Transform _popupPos;
    private CanvasInputWorld instantatiatedCanvas;
    private UIPopup instantatiatedPopup;
    public RecipeBaseSO fixRecipe;
    public event Action PopupDataChanged;
    public SpriteRenderer SpriteRenderer;
    private bool _currentlyInteracting;

    public Sprite InteractIcon => FixIcon;

    private bool _isFixed = false;
    private bool _canInteract = true;
    public bool IsFixed => _isFixed;


    bool IInteractable.CanInteract {
        get {
            return _canInteract && !_isFixed; // Can't interact if fixed. Keeping the script so we can properly initialize it
        }
        set {
            SetInteractable(value); // This seems like we are cheating but it should work
        }
    }

    public Vector2 LookaheadPos => Vector2.zero;

    private void Start() {
        // Create a copy of the material
        SpriteRenderer.material = new Material(SpriteRenderer.material);
        SetMaterialBrokenBool(!_isFixed); // Fixed is inverted because fixed != broken
    }


    public void SetMaterialBrokenBool(bool isBroken) {
        SpriteRenderer.material.SetInt("_Damaged", isBroken ? 1 : 0);
    }

    public void SetFixed(bool isFixed) {
        // Set visuals locally
        SetMaterialBrokenBool(!isFixed); // Fixed is inverted because fixed != broken
        if (_currentlyInteracting) {
            // If we are currently interacting with this entity, we should close the popup
            if (instantatiatedCanvas != null) {
                Destroy(instantatiatedCanvas.gameObject);
                instantatiatedCanvas = null;
                _currentlyInteracting = false;
            }
        }
        _isFixed = isFixed;
    }

    public void SetInteractable(bool isInteractable, Sprite interactPrompt = null) {
        if (isInteractable) {
            instantatiatedCanvas = Instantiate(App.ResourceSystem.GetPrefab("CanvasInputWorld"), _popupPos.position,Quaternion.identity,transform).GetComponent<CanvasInputWorld>();
            instantatiatedCanvas.Init(this, interactPrompt);
        } else {
            if (instantatiatedCanvas != null) {
                Destroy(instantatiatedCanvas.gameObject);
            }
            _currentlyInteracting = false;
        }
    }

    // The best would be to use the already existing popup manager to setup the thing but I don't know what the real benefits are atm, this works for now
    public void Interact(PlayerManager player) {
        if(instantatiatedCanvas != null && instantatiatedPopup == null) {
            if (_isFixed) {
                // Open the UI for this object?
                return; 
            }
            _currentlyInteracting = true;
            var clientInventory = SubmarineManager.Instance.SubInventory; // Probably really bad to do this but EH?
            instantatiatedPopup = Instantiate(App.ResourceSystem.GetPrefab("Popup"), instantatiatedCanvas.transform).GetComponent<UIPopup>();
            instantatiatedPopup.transform.localScale = Vector3.one * 0.025f;
            instantatiatedPopup.SetData(new(fixRecipe.displayName, fixRecipe.description, fixRecipe.GetIngredientStatuses(clientInventory)));
            instantatiatedCanvas.SetPromptNextStage(instantatiatedPopup.transform);
        } else if(instantatiatedPopup != null) {
            // Basically pressing again while the popup is already open
            // TODO use PopupManager.CurrentPopup!!
            // Passing the instantiated popup so we can show visual feedback BTW, this should probably be handled by PopupManager, it already has a CurrentPopup variable
            
            //var context = new RecipeExecutionContext { Player = client.GetComponent<NetworkedPlayer>()};
            var context = ExecutionContext.FromObject(gameObject);
            player.CraftingComponent.AttemptCraft(fixRecipe, context, instantatiatedPopup);
        }
        //PopupManager.Instance.TryShowWorldPopup(this,client);
    }

    public PopupData GetPopupData(InventoryManager clientInv) {
        return new(fixRecipe.name, fixRecipe.description, fixRecipe.GetIngredientStatuses(clientInv));
    }

}