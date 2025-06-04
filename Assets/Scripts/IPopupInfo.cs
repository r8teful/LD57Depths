using System;
using System.Collections.Generic;

// Anything that can give information to the popup on the cursor
public interface IPopupInfo {
    PopupData GetPopupData();
    event Action PopupDataChanged;
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