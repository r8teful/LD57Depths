using FishNet.Object;
using System;
using UnityEditor.PackageManager;
using UnityEngine;

[RequireComponent (typeof(BoxCollider2D))]
public class FixableEntity : MonoBehaviour, IInteractable, IPopupInfo {
    public SpriteRenderer SpriteRenderer;
    public RecipeBaseSO fixRecipe;
    [SerializeField] private Sprite FixIcon;
    private CanvasInputWorld instantatiatedCanvas;
    private UIPopup instantatiatedPopup;

    public event Action PopupDataChanged;

    public Sprite InteractIcon => FixIcon;

    private void Start() {
        // Create a copy of the material
        SpriteRenderer.material = new Material(SpriteRenderer.material);
    }
    public void SetIsBrokenBool(bool isBroken) {
        SpriteRenderer.material.SetInt("_Damaged", isBroken ? 1 : 0);
    }
    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.gameObject.CompareTag("Player")) {
        Debug.Log("ENTER!");
            //SubInside.Instance.EnterExit();
        }
    }
    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.gameObject.CompareTag("Player")) {
        Debug.Log("EXIT!");
            //SubInside.Instance.ExitCollider();
        }
    }

    public void SetInteractable(bool isInteractable, Sprite interactPrompt = null) {
        if (isInteractable) {
            instantatiatedCanvas = Instantiate(App.ResourceSystem.GetPrefab("CanvasInputWorld"), transform).GetComponent<CanvasInputWorld>();
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
            var clientInventory = client.GetComponent<NetworkedPlayerInventory>().GetInventoryManager(); // Probably really bad to do this but EH?
            instantatiatedPopup = Instantiate(App.ResourceSystem.GetPrefab("PopupWorld"), instantatiatedCanvas.transform).GetComponent<UIPopup>();
            instantatiatedPopup.SetData(new(fixRecipe.name, fixRecipe.description, fixRecipe.GetIngredientStatuses(clientInventory)));
            instantatiatedCanvas.SetPromptNextStage(instantatiatedPopup.transform);
        } else if(instantatiatedPopup != null) {
            // Basically pressing again while the popup is already open
            Debug.Log("trying to fix!!");
            // Should not not happen really
            UICraftingManager.Instance.AttemptCraft(fixRecipe);
        }
        //PopupManager.Instance.TryShowWorldPopup(this,client);
    }

    public PopupData GetPopupData(GameObject obj) {
        var clientInventory = obj.GetComponent<NetworkedPlayerInventory>().GetInventoryManager(); // Probably really bad to do this but EH?
        return new(fixRecipe.name, fixRecipe.description, fixRecipe.GetIngredientStatuses(clientInventory));
    }
}