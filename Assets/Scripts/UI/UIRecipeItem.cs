// --- Example RecipeDisplayItem.cs (on your recipeUIPrefab) ---
using System;
using System.Collections.Generic;
using TMPro;
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
    private PopupManager _popupManager;

    public event Action PopupDataChanged;

    public PopupData GetPopupData(GameObject obj = null) {
        return popupRecipeData; // Updatestatus gets called which edits this and ensures we have the right data
    }

    public void Init(RecipeBaseSO recipe, InventoryManager clientInventory, UICraftingManager craftingUI,PopupManager popupManager) {
        _recipe = recipe;
        _craftingUIController = craftingUI;
        _popupManager = popupManager;
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
        _popupManager.OnPointerEnterItem(this);
    }

    public void OnPointerExit(PointerEventData eventData) {
        _popupManager.OnPointerExitItem();
    }

    // We need to know if we can craft the item when picking up an item
    public void UpdateStatus(InventoryManager clientInventory) {
        if (_recipe == null || clientInventory == null)
            return;

        List<IngredientStatus> statuses = _recipe.GetIngredientStatuses(clientInventory);
        bool canAffordAll = true;
        popupRecipeData = new(_recipe.displayName, _recipe.description, statuses);
        foreach (var status in statuses) {
            if (!status.HasEnough) {
                canAffordAll = false;
            }
        }
        PopupDataChanged?.Invoke();
        craftButton.interactable = canAffordAll; // Enable button only if all ingredients met locally
    }

    void OnCraftButtonClicked() {
        _craftingUIController.AttemptCraft(_recipe);
    }
}