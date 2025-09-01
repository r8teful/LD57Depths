using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Used in the control panel overview, hover over it to see the upgrade, if you click you get sent to the upgrade tab
public class UISubUpgradeIcon : MonoBehaviour, IPopupInfo, IPointerEnterHandler, IPointerExitHandler {
    public event Action PopupDataChanged;
    [SerializeField] private SubRecipeSO _recipeData;
    public PopupData GetPopupData(InventoryManager inv) {
        // We can alternativaly enter the resource amount here if we'd like

        // TODO also we should set the icon to the current "tier" of the upgrade we are in
        return new PopupData(_recipeData.displayName, _recipeData.description, null, _recipeData.UpgradeIconSteps[0]);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        PopupManager.Instance.ShowPopup(this, true);
    }

    public void OnPointerExit(PointerEventData eventData) {
        PopupManager.Instance.ShowPopup(this, false);
    }
}
