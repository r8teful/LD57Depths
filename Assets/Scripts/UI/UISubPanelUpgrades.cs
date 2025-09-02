using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISubPanelUpgrades : MonoBehaviour {
    [SerializeField] private Image _upgradeStatusImage;
    [SerializeField] private TextMeshProUGUI _upgradeName;
    [SerializeField] private Transform _upgradeBarContainer;
    private void OnEnable() {
        // Fetch the current upgrade & its state
        ushort curRecipe = SubmarineManager.Instance.CurrentRecipe;
        var recipeData = App.ResourceSystem.GetRecipeByID(curRecipe);
        var upgradeData = SubmarineManager.Instance.UpgradeData;
        int upgradeIndex = SubmarineManager.Instance.GetUpgradeIndex(curRecipe);
        if(recipeData is SubRecipeSO subRecipeData) {
            _upgradeStatusImage.sprite = subRecipeData.UpgradeIconSteps[upgradeIndex];
            _upgradeName.name = subRecipeData.displayName;
            // Now here we want to instantiate the progress bars, and initialize them with how many resources we have
            foreach (var reqItem in recipeData.requiredItems) {
                int requiredAmount = Mathf.CeilToInt((float)reqItem.quantity/8); // Maybe later this wont be 8, also idealy this would need to be divisable by 8 
                var ingredientStatus = new IngredientStatus(reqItem.item, requiredAmount,
                    NetworkedPlayer.LocalInstance.GetInventory().GetItemCount(reqItem.item.ID));
                Instantiate(App.ResourceSystem.GetPrefab<UISubUpgradeBar>("UISubUpgradeBar"), _upgradeBarContainer).Init(ingredientStatus,reqItem.quantity);
            }
            // Then when we press the contribute button, we call SubmarineManager to add that to the Data, Then the data will change
            // And so will the bars because of the OnUpgradeDataChanged event
        }

    }
}