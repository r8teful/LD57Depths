using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

public class UIUpgradeScreen : MonoBehaviour {
    [SerializeField] private GameObject _upgradePanel;
    private RectTransform _upgradePanelRect;
    [SerializeField] private GameObject _upgradePanelTree;
    [SerializeField] private UpgradeTreeController _upgradeTreeController;
    public UpgradePanAndZoom PanAndZoom;
    private UIManager _UIManagerParent;
    private UpgradeTreeDataSO _treeData;
    public UIUpgradeTree UpgradeTreeInstance { get; private set; }
    public bool IsOpen => _upgradePanel.activeSelf;
    
    public UIManager GetUIManager() => _UIManagerParent;
    public static event Action<UpgradeNodeSO> OnSelectedNodeChanged; // Used to show correct stats 
    public event Action<bool> OnPanelChanged;
    private void Awake() {
        _upgradePanelRect = _upgradePanel.GetComponent<RectTransform>();
    }        
    private void Start() {
        _upgradePanel.SetActive(false); // Start with the panel being hidden
        _upgradePanelTree.SetActive(true);
        UIUpgradeTree.OnUpgradeButtonPurchased += OnUpgradePurchasedThroughTree;
    }
    // todo make this good the shake is fucked it doesn't go back to where it started
    private void OnUpgradePurchasedThroughTree() {
        Debug.Log("PURSHASETREE");
        var rect = _upgradePanel.GetComponent<RectTransform>();
       // rect.DOShakeAnchorPos(0.2f,60,2,180,randomnessMode:ShakeRandomnessMode.Harmonic);
       // rect.DOShakeRotation(0.4f,60,2,180,randomnessMode:ShakeRandomnessMode.Harmonic);
        //rect.DOComplete();
    }
    internal void Init(UIManager UIManager, PlayerManager client) {
        _UIManagerParent = UIManager;
        _treeData = App.ResourceSystem.GetTreeByName(GameSetupManager.Instance.GetUpgradeTreeName()); // This will obviously have to come from some sort of "game selection" manager
       
        UpgradeTreeInstance = InstantiateTree(_treeData, _upgradePanelTree.transform, client);
        PanAndZoom.Init(client.InputManager);
        _upgradeTreeController.Init(client, UpgradeTreeInstance);    
    }
    private UIUpgradeTree InstantiateTree(UpgradeTreeDataSO treeData, Transform transformParent, PlayerManager player) {
        if (treeData == null) {
            Debug.LogError("Could not find tree!");
            return null;
        }
        if (treeData.prefab == null) {
            Debug.LogWarning($"{treeData.treeName} does not have a corresponding UI tree, skipping...");
            return null;
        }
        var treeObj = Instantiate(treeData.prefab, transformParent);
        treeObj.Init(treeData, player);
        treeObj.name = $"UpgradeTree_{treeData.treeName}";
        return treeObj;
    }
    public void PanelToggle() {
        var isActive = !_upgradePanel.activeSelf;
        if (isActive) {
            _upgradePanel.SetActive(true);
            // we're now open
            _upgradePanelRect.localScale = new(1, 0.2f, 1);
            // _upgradePanelRect.DOScaleY(1, 0.6f).SetEase(Ease.OutElastic);
            _upgradePanelRect.DOScaleY(1, 0.2f).SetEase(Ease.OutBack);
            _upgradeTreeController.OnTreeOpen();
        } else {
            _upgradePanelRect.localScale = Vector3.one;
            _upgradePanelRect.DOScaleY(0.2f, 0.05f).SetEase(Ease.OutCubic).
                OnComplete(() => {
                    _upgradePanel.SetActive(false);
                    UpgradeTreeInstance.OnTreeCloseFinish();
                });
            _upgradeTreeController.OnTreeClose();
            UpgradeTreeInstance.OnTreeClose();
            // close
        }
        OnPanelChanged?.Invoke(isActive);
    }

    internal void PanelHide() {
        _upgradePanel.SetActive(false);
    }
}
