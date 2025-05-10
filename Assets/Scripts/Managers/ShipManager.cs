using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShipManager : StaticInstance<ShipManager> {
    public Transform upgradeContainer;
    public TextMeshProUGUI progressText;
    public CanvasGroup ShopMenuGroup;
    public CanvasGroup ShipMenuGroup;
    public Image SchematicImage;
    public List<GameObject> _instantiatedShopElements = new List<GameObject>();
    private bool _isShopOpen;
    private bool _isShipOpen;
    private int _repairProgress;
    public List<UIRepairs> repairs;
    private Dictionary<RepairType, bool> repairProgress = new Dictionary<RepairType, bool>();
    protected override void Awake() {
        base.Awake();
        UpgradeManager.UpgradeBought += OnUpgradeBought;
        foreach (UIRepairs upgrade in repairs) {
            repairProgress[upgrade.repairData.RepairType] = false; // Start false
            upgrade.IsComplete = false;
        }
    }

    private void OnDestroy() {
        UpgradeManager.UpgradeBought -= OnUpgradeBought;
    }
    private void Start() {
        RefreshShop();
        ShipMenuGroup.interactable = false;
        ShipMenuGroup.blocksRaycasts = false;
        ShipMenuGroup.alpha = 0;
        ShopMenuGroup.interactable = false;
        ShopMenuGroup.blocksRaycasts = false;
    }
    private void OnUpgradeBought(UpgradeType t) {
        RefreshShop();
    }

    public void ShopOpen() {
        if (_isShopOpen) return;
        _isShopOpen = true;
        RefreshShop();
        ShopMenuGroup.interactable = true;
        ShopMenuGroup.blocksRaycasts = true;
        ShopMenuGroup.DOFade(1, 0.2f);
    }
    public void ShopClose() {
        if (!_isShopOpen) return;
        _isShopOpen = false;
        ShopMenuGroup.interactable = false;
        ShopMenuGroup.blocksRaycasts = false;
        ShopMenuGroup.DOFade(0, 0.2f);
    }
    public void ShipOpen() {
        if (_isShipOpen) return;
        _isShipOpen = true;
        InstantiateShipRepair();
        ShipMenuGroup.interactable = true;
        ShipMenuGroup.blocksRaycasts = true;
        ShipMenuGroup.alpha = 1;
    }
    public void ShipClose() {
        if (!_isShipOpen) return;
        _isShipOpen = false;
        ShipMenuGroup.interactable = false;
        ShipMenuGroup.blocksRaycasts = false;
        ShipMenuGroup.alpha = 0;
    }
    public void RefreshShop() {
        foreach(var ins in _instantiatedShopElements) {
            Destroy(ins);
        }
        _instantiatedShopElements.Clear();
        foreach(var upgrade in UpgradeManager.Instance.upgrades) {
            var i = Instantiate(Resources.Load<UIUpgrade>("UI/UIUpgrade"), upgradeContainer);
            i.Init(upgrade);
            _instantiatedShopElements.Add(i.gameObject);
        }
    }
    private void InstantiateShipRepair() {
        foreach (var repair in repairs) {
            repair.SetRepairDataCost(); // each repair holds its own data so we don't need to pass it here
        }
        SchematicImage.sprite = Resources.Load<Sprite>($"UI/SchematicsScreen{GetRepairProgress()}");
    }

    internal void RepairPressed(RepairType type, UpgradeCostSO[] costData) {
        var d = new Dictionary<ItemData, int>();
        foreach(var data in costData) {
            d.Add(data.resourceType,Mathf.FloorToInt(data.baseCost));
        }
        if (UpgradeManager.Instance.TryRemoveResources(d, out var b)) {
            repairProgress[type] = true;
            foreach (var repair in repairs) {
                if(repair.repairData.RepairType == type) {
                    // Destroy because we did the upgrade
                    Destroy(repair.gameObject);
                    repairs.Remove(repair);
                    break;
                }
            }
            // Do something cool?
            UpdateProgressCounter();
        }
    }
    private void UpdateProgressCounter() {
        progressText.text = $"{GetRepairProgress()}/3";
    }
    public int GetRepairProgress() {
        int p = 0;
        foreach (var d in repairProgress) {
            if (d.Value) p++;
        }
        return p;
    }
    public bool IsAnyMenuOpen() => _isShopOpen || _isShipOpen;
}