using System;
using System.Collections.Generic;
using UnityEngine;

// Anything that can give information to the popup on the cursor
public interface IPopupInfo {
    PopupData GetPopupData(InventoryManager inv);
    event Action PopupDataChanged;
}

[System.Serializable]
public class PopupData {
    public string title;
    public string description;
    public Sprite Icon;
    public List<IngredientStatus> craftingInfo; // int is quantity
    public List<StatChangeStatus> upgradeEffects; // in its core this is what we want to know
    public NodeProgressionStatus progressionInfo;

    public PopupData(string title, string description, List<IngredientStatus> craftingInfo, Sprite icon = null, List<StatChangeStatus> statInfo = null,
        NodeProgressionStatus progressionInfo = default) {
        this.title = title;
        this.description = description;
        this.craftingInfo = craftingInfo;
        this.upgradeEffects = statInfo;
        this.progressionInfo = progressionInfo;
        Icon = icon;
    }
}