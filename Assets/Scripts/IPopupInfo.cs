using System;
using System.Collections.Generic;
using UnityEngine;

// Anything that can give information to the popup on the cursor
public interface IPopupInfo {
    PopupData GetPopupData(InventoryManager inv);
    event Action PopupDataChanged;
    event Action<IPopupInfo,bool> OnPopupShow;
}

[System.Serializable]
public class PopupData {
    public string title;
    public string description;
    public List<IngredientStatus> craftingInfo; // int is quantity

    public PopupData(string title, string description, List<IngredientStatus> craftingInfo) {
        this.title = title;
        this.description = description;
        this.craftingInfo = craftingInfo;
    }
}