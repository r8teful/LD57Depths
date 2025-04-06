using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class UIShopManager : StaticInstance<UIShopManager> {
    public Transform upgradeContainer;
    public CanvasGroup ShopMenuGroup;
    public List<GameObject> _instantiated = new List<GameObject>();
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
        RefreshShop();
        ShopMenuGroup.interactable = true;
        ShopMenuGroup.blocksRaycasts = true;
        ShopMenuGroup.DOFade(1, 0.2f);
    }
    public void ShopClose() {
        ShopMenuGroup.interactable = false;
        ShopMenuGroup.blocksRaycasts = false;
        ShopMenuGroup.DOFade(0, 0.2f);
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