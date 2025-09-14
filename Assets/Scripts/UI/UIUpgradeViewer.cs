using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIUpgradeViewer : MonoBehaviour {

    [SerializeField] private TextMeshProUGUI _statHeaderText;
    [SerializeField] private Transform _statContainer;
    [SerializeField] private Transform _costContainer;
    [SerializeField] private Button _buttonBuyUpgrade;
    private UpgradeRecipeSO _shownRecipe;
    private void Start() {
        UIUpgradeScreen.OnTabChanged += OnTabViewChanged;
        UIUpgradeScreen.OnSelectedUpgradeChanged += OnUpgradeChanged;
        _buttonBuyUpgrade.onClick.AddListener(OnBuyClick);
        DestroyAllChildren();
    }
    private void OnDestroy() {
        UIUpgradeScreen.OnTabChanged -= OnTabViewChanged;
        UIUpgradeScreen.OnSelectedUpgradeChanged -= OnUpgradeChanged;
        _buttonBuyUpgrade.onClick.RemoveListener(OnBuyClick);

    }

    private void OnBuyClick() {
        UpgradeManagerPlayer.Instance.TryPurchaseUpgrade(_shownRecipe);
    }

    private void OnUpgradeChanged(UpgradeRecipeSO upgradeData) {
        _shownRecipe = upgradeData;
        // Show the upgrade in the view
        DestroyAllChildren();
        foreach (var effect in upgradeData.effects) {
            if(effect is StatUpgradeEffectSO e) {
                var stat = e.upgradeType;
                var currentValue = NetworkedPlayer.LocalInstance.PlayerStats.GetStat(stat);
                var nextValue = UpgradeCalculator.CalculateUpgradeIncrease(currentValue, e.increaseType, e.modificationValue);
                Instantiate(App.ResourceSystem.GetPrefab<UIUpgradeStat>("UIUpgradeStat"), _statContainer).Init(stat, currentValue,nextValue);
            }
        }
        foreach (var ingredient in upgradeData.GetIngredientStatuses(NetworkedPlayer.LocalInstance.GetInventory())) {
            Instantiate(App.ResourceSystem.GetPrefab<UIIngredientVisual>("UIIngredientVisual"), _costContainer).Init(ingredient);
        }
    }

    private void OnTabViewChanged(UpgradeTreeDataSO tree) {
        ChangeStatView(tree);
    }

    private void ChangeStatView(UpgradeTreeDataSO data) {
        _statHeaderText.text = data.treeName;
        for (int i = 0; i < _statContainer.childCount; i++) {
            Destroy(_statContainer.GetChild(i).gameObject);
        }
        foreach (var stat in data.statsToDisplay) {
            // Get the value of the stat from the StatManager
        }
    }
    private void DestroyAllChildren() {
        for (int i = 0; i < _statContainer.childCount; i++) {
            Destroy(_statContainer.GetChild(i).gameObject);
        }
        for (int i = 0; i < _costContainer.childCount; i++) {
            Destroy(_costContainer.GetChild(i).gameObject);
        }
    }
}