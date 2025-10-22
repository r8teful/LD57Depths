using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIUpgradeViewer : MonoBehaviour {

    [SerializeField] private TextMeshProUGUI _statHeaderText;
    [SerializeField] private Transform _statContainer;
    [SerializeField] private Transform _costContainer;
    [SerializeField] private Transform _backgroundObject;
    [SerializeField] private Button _buttonBuyUpgrade;
    private UpgradeRecipeSO _shownRecipe;
    private UpgradeNodeSO _selectedUpgradeNode;
    private void Start() {
        UIUpgradeScreen.OnTabChanged += OnTabViewChanged;
        UIUpgradeScreen.OnSelectedNodeChanged += OnUpgradeChanged;
        _buttonBuyUpgrade.onClick.AddListener(OnBuyClick);
        DestroyAllChildren();
    }
    private void OnDestroy() {
        UIUpgradeScreen.OnTabChanged -= OnTabViewChanged;
        UIUpgradeScreen.OnSelectedNodeChanged -= OnUpgradeChanged;
        _buttonBuyUpgrade.onClick.RemoveListener(OnBuyClick);

    }

    private void OnBuyClick() {
        UpgradeManagerPlayer.LocalInstance.TryPurchaseUpgrade(_selectedUpgradeNode);
    }

    private void OnUpgradeChanged(UpgradeNodeSO node) {
        UpgradeRecipeSO upgradeData = null; // TODO
        _selectedUpgradeNode = node;
        _shownRecipe = upgradeData;
        _statHeaderText.text = upgradeData.displayName;
        // Show the upgrade in the view
        DestroyAllChildren();
        foreach (var effect in upgradeData.effects) {
            if(effect is StatUpgradeEffectSO e) {
                var stat = e.upgradeType;
                var currentValue = NetworkedPlayer.LocalInstance.PlayerStats.GetStat(stat);
                var nextValue = UpgradeCalculator.CalculateUpgradeChange(currentValue, e.increaseType, e.modificationValue);
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
        App.AudioController.PlaySound2D("ScreenChange");
        _statHeaderText.text = data.treeName;
        for (int i = 0; i < _statContainer.childCount; i++) {
            Destroy(_statContainer.GetChild(i).gameObject);
        }
        foreach (var stat in data.statsToDisplay) {
            // Get the value of the stat from the StatManager
            var currentValue = NetworkedPlayer.LocalInstance.PlayerStats.GetStat(stat);
            Instantiate(App.ResourceSystem.GetPrefab<UIUpgradeStat>("UIUpgradeStat"), _statContainer).Init(stat, currentValue);
        }
    }
    private void DestroyAllChildren() {
        for (int i = 0; i < _statContainer.childCount; i++) {
            Destroy(_statContainer.GetChild(i).gameObject);
        }
        for (int i = 0; i < _costContainer.childCount; i++) {
            if (_costContainer.GetChild(i).GetInstanceID() == _backgroundObject.GetInstanceID())
                continue; // Don't destroy the backgroundObject
            Destroy(_costContainer.GetChild(i).gameObject);
        }
    }
}