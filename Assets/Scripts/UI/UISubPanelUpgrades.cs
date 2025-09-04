using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISubPanelUpgrades : MonoBehaviour {
    [SerializeField] private Image _upgradeStatusImage;
    [SerializeField] private TextMeshProUGUI _upgradeName;
    [SerializeField] private Transform _upgradeBarContainer;
    private Dictionary<ushort,UISubUpgradeBar> _upgradeBars = new Dictionary<ushort, UISubUpgradeBar>(); // Runtime instantiated bars
    private SubRecipeSO _curRecipeData;
    private void Awake() {
        SubmarineManager.Instance.OnUpgradeDataChanged += UpgradeDataChanged;
        SubmarineManager.Instance.OnCurRecipeChanged += CurRecipeChanged;
        // Fetch the current upgrade & its state
        ushort curRecipe = SubmarineManager.Instance.CurrentRecipe;
        var recipeData = App.ResourceSystem.GetRecipeByID(curRecipe);
        _curRecipeData = recipeData as SubRecipeSO;
        InitializeUpgradeBars();
        UpdatePanelVisuals();
    }

 

    // Initializes the empty state of a recipeBar.
    private void InitializeUpgradeBars() {
        // Delete existing 
        for (int i = 0; i < _upgradeBarContainer.childCount; i++) {
            Destroy(_upgradeBarContainer.GetChild(i).gameObject);
        }
        _upgradeBars.Clear();

        // Now here we want to instantiate the progress bars, and initialize them with how many resources we have
        foreach (var reqItem in _curRecipeData.requiredItems) {
            // RequiredAmount is how much is contributed when pressing the button
            int requiredAmount = Mathf.CeilToInt((float)reqItem.quantity / 10); // Maybe later this wont be 8, also idealy this would need to be divisable by 8 
            var ingredientStatus = new IngredientStatus(reqItem.item, requiredAmount,
                NetworkedPlayer.LocalInstance.GetInventory().GetItemCount(reqItem.item.ID));
            var contributed = SubmarineManager.Instance.GetContributedAmount(_curRecipeData.ID, reqItem.item.ID);
            var bar = Instantiate(App.ResourceSystem.GetPrefab<UISubUpgradeBar>("UISubUpgradeBar"), _upgradeBarContainer);
            bar.Init(_curRecipeData, ingredientStatus, contributed, reqItem.quantity);
            _upgradeBars.Add(reqItem.item.ID, bar);
        }
    }

    private void UpgradeDataChanged(ushort recipeChangedID) {
        UpdateBarVisuals();
        UpdatePanelVisuals();
    }
    private void CurRecipeChanged(ushort id) {
        SubRecipeSO changedRecipe = App.ResourceSystem.GetRecipeByID(id) as SubRecipeSO;
        if (_curRecipeData != changedRecipe) {
            // delete old subupgradeBars, and instatiate new ones
            _curRecipeData = changedRecipe;
            InitializeUpgradeBars();
            UpdatePanelVisuals();
        }
    }

    private void UpdatePanelVisuals() {
        int upgradeIndex = SubmarineManager.Instance.GetUpgradeIndex(_curRecipeData.ID);
        _upgradeStatusImage.sprite = _curRecipeData.UpgradeIconSteps[upgradeIndex];
        _upgradeName.text = _curRecipeData.displayName;
    }

    private void UpdateBarVisuals() {
        // send the new IngredientStatus, and item totals to all upgrade bars... 
        foreach (var reqItem in _curRecipeData.requiredItems) {
            int requiredAmount = Mathf.CeilToInt((float)reqItem.quantity / 10); // Have to find a better way to get this
            var ingredientStatus = new IngredientStatus(reqItem.item,requiredAmount,
            NetworkedPlayer.LocalInstance.GetInventory().GetItemCount(reqItem.item.ID));
            var contributed = SubmarineManager.Instance.GetContributedAmount(_curRecipeData.ID, reqItem.item.ID);
            _upgradeBars[reqItem.item.ID].SetNewData(ingredientStatus, contributed); // Easy lookup now that we've mapped the item ID to each bar
        }
    }
}