// --- Example RecipeDisplayItem.cs (on your recipeUIPrefab) ---
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UIRecipeItem : MonoBehaviour, IPopupInfo, IPointerEnterHandler, IPointerExitHandler {
    public Image recipeIconImage;
    public Button craftButton;

    private RecipeBaseSO _recipe;
    private UICrafting _craftingUIController;
    private PopupData recipeData;
    private PopupManager _popupManager;
    public PopupData GetPopupData() {
        return recipeData;
    }

    public void Init(RecipeBaseSO recipe, InventoryManager clientInventory, UICrafting craftingUI,PopupManager popupManager) {
        _recipe = recipe;
        _craftingUIController = craftingUI;
        _popupManager = popupManager;
        recipeData = new PopupData(recipe.displayName, recipe.description, null);
        if (recipeIconImage != null && recipe.recipeIcon != null) {
            recipeIconImage.sprite = recipe.recipeIcon;
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

    public void UpdateStatus(InventoryManager clientInventory) {
        if (_recipe == null || clientInventory == null)
            return;

        System.Text.StringBuilder sb = new System.Text.StringBuilder("Requires:\n");
        List<IngredientStatus> statuses = _recipe.GetIngredientStatuses(clientInventory);
        bool canAffordAll = true;

        foreach (var status in statuses) {
            string color = status.HasEnough ? "green" : "red";
            sb.AppendLine($"<color={color}>{status.Item.itemName}: {status.CurrentAmount}/{status.RequiredAmount}</color>");
            if (!status.HasEnough) {
                canAffordAll = false;
            }
        }
        craftButton.interactable = canAffordAll; // Enable button only if all ingredients met locally
    }

    void OnCraftButtonClicked() {
        _craftingUIController.AttemptCraft(_recipe);
    }
}