using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class UIShopManager : StaticInstance<UIShopManager> {
    public Transform upgradeContainer;
    public CanvasGroup ShopMenuGroup;
    public CanvasGroup ShipMenuGroup;
    public List<GameObject> _instantiated = new List<GameObject>();
    private bool _isShopOpen;
    private bool _isShipOpen;
    protected override void Awake() {
        base.Awake();
        UpgradeManager.UpgradeBought += OnUpgradeBought;
    }
    private void OnDestroy() {
        UpgradeManager.UpgradeBought -= OnUpgradeBought;
    }
    private void Start() {
        RefreshShop();
    }
    private void OnUpgradeBought() {
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
        foreach(var ins in _instantiated) {
            Destroy(ins);
        }
        _instantiated.Clear();
        foreach(var upgrade in UpgradeManager.Instance.upgrades) {
            var i = Instantiate(Resources.Load<UIUpgrade>("UI/UIUpgrade"), upgradeContainer);
            i.Init(upgrade);
            _instantiated.Add(i.gameObject);
        }
    }
}