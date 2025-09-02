using System.Collections.Generic;
using UnityEngine;

public class UISubPanelOverview : MonoBehaviour {
    [SerializeField] UISubUpgradeIcon[] upgradeIcons; // IMPORTANT!! Order matters

    private void Start() {
        // Init the upgradeIcons with their state Available/Unavailable/Upgraded
        for (int i = 0; i < upgradeIcons.Length; i++) {
            // Do this if no save data exists, otherwise, load the save data and set it like that
            upgradeIcons[i].Init(this, i == 0 ? UISubUpgradeIcon.SubUpgradeState.Available : UISubUpgradeIcon.SubUpgradeState.Unavailable);
            
        }
    }
    public SubRecipeSO GetSubRecipeData(int upgradeIndex) {
        return upgradeIcons[upgradeIndex].RecipeData;
    }
}