// --- Example RecipeDisplayItem.cs (on your recipeUIPrefab) ---
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Recipe icon in the recipe tab in the crafting menu
public class UIRecipeItem : MonoBehaviour, IPopupInfo, IPointerEnterHandler, IPointerExitHandler {
    public Image recipeIconImage;
    public Button craftButton;

    private RecipeBaseSO _recipe;
    private UICraftingManager _craftingUIController;
    private PopupData popupRecipeData;

    public event Action PopupDataChanged;
    public event Action<IPopupInfo, bool> OnPopupShow;

    public PopupData GetPopupData(InventoryManager clientInv) {
        return popupRecipeData; // Updatestatus gets called which edits this and ensures we have the right data
    }

    public void Init(RecipeBaseSO recipe, InventoryManager clientInventory, UICraftingManager craftingUI) {
        _recipe = recipe;
        _craftingUIController = craftingUI;
        if (recipeIconImage != null && recipe.icon != null) {
            recipeIconImage.sprite = recipe.icon;
            recipeIconImage.enabled = true;
        }
        if (craftButton != null) {
            craftButton.onClick.RemoveAllListeners();
            craftButton.onClick.AddListener(OnCraftButtonClicked);
        }

        UpdateStatus(clientInventory);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        OnPopupShow?.Invoke(this,true);
    }

    public void OnPointerExit(PointerEventData eventData) {
        OnPopupShow?.Invoke(this,false);
    }

    // We need to know if we can craft the item when picking up an item
    public void UpdateStatus(InventoryManager clientInventory) {
        if (_recipe == null || clientInventory == null)
            return;

        List<IngredientStatus> statuses = _recipe.GetIngredientStatuses(clientInventory);
        popupRecipeData = new(_recipe.displayName, _recipe.description, statuses);
        PopupDataChanged?.Invoke();
        
        // Don't really want this because it will not allow us any feedback if we do press when we can't afford
        //bool canAffordAll = true;
        //foreach (var status in statuses) {
        //    if (!status.HasEnough) {
        //        canAffordAll = false;
        //    }
        //}
        //craftButton.interactable = canAffordAll; // Enable button only if all ingredients met locally
    }

    void OnCraftButtonClicked() {
        Debug.LogWarning("Not implemented!");
        //_craftingUIController.AttemptCraft(_recipe,_popupManager.CurrentPopup, null);
    }
}