using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIUpgradeScreen : MonoBehaviour {
    [SerializeField] private GameObject _upgradePanel;
    private RectTransform _upgradePanelRect;
    [SerializeField] private GameObject _upgradePanelTree;
    //public UpgradePanAndZoom PanAndZoom;
    public PanAndZoomController PanAndZoom;
    private UIManager _UIManagerParent;
    private UpgradeTreeDataSO _treeData;
    public UIUpgradeTree UpgradeTreeInstance { get; private set; }
    public bool IsOpen => _upgradePanel.activeSelf;
    
    public UIManager GetUIManager() => _UIManagerParent;
    public event Action<bool> OnPanelChanged;
    private void Awake() {
        _upgradePanelRect = _upgradePanel.GetComponent<RectTransform>();
    }        
    private void Start() {
        _upgradePanel.SetActive(false); // Start with the panel being hidden
        _upgradePanelTree.SetActive(true);
        UIUpgradeTree.OnUpgradeButtonPurchased += OnUpgradePurchasedThroughTree;
    }
    private void OnDestroy() {
        UIUpgradeTree.OnUpgradeButtonPurchased -= OnUpgradePurchasedThroughTree;
    }
    // todo make this good the shake is fucked it doesn't go back to where it started
    private void OnUpgradePurchasedThroughTree() {
        Debug.Log("PURSHASETREE");
        var rect = _upgradePanel.GetComponent<RectTransform>();
        rect.DOShakePosition(0.2f,strength: 4,vibrato: 25,randomness:100,randomnessMode:ShakeRandomnessMode.Harmonic);
        
        //rect.DOShakeAnchorPos(0.2f,30,2,180,randomnessMode:ShakeRandomnessMode.Harmonic);
       // rect.DOShakeRotation(0.4f,60,2,180,randomnessMode:ShakeRandomnessMode.Harmonic);
        //rect.DOComplete();
    }
    internal void Init(UIManager UIManager, PlayerManager client) {
        _UIManagerParent = UIManager;
        _treeData = App.ResourceSystem.GetTreeByName(GameManager.Instance.GetUpgradeTreeName()); // This will obviously have to come from some sort of "game selection" manager
       
        UpgradeTreeInstance = InstantiateTree(_treeData, _upgradePanelTree.transform, client);
        //PanAndZoom.Init(client.InputManager);
        PanAndZoom.Init(client.InputManager, UpgradeTreeInstance);
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
    public void PanelOpen() {
        _upgradePanel.SetActive(true);
        // we're now open
        _upgradePanelRect.localScale = new(1, 0.2f, 1);
        // _upgradePanelRect.DOScaleY(1, 0.6f).SetEase(Ease.OutElastic);
        _upgradePanelRect.DOScaleY(1, 0.2f).SetEase(Ease.OutBack);
        UpgradeTreeInstance.OnTreeOpen();
        EventSystem.current.SetSelectedGameObject(UpgradeTreeInstance.LastSelectedNode() != null ? UpgradeTreeInstance.LastSelectedNode().gameObject : null);
        OnPanelChanged?.Invoke(true);
    }

    internal void PanelClose() {
        _upgradePanelRect.localScale = Vector3.one;
        _upgradePanelRect.DOScaleY(0.2f, 0.05f).SetEase(Ease.OutCubic).
            OnComplete(() => {
                _upgradePanel.SetActive(false);
                UpgradeTreeInstance.OnTreeCloseFinish();
            });
        UpgradeTreeInstance.OnTreeClose();
        if (UISelectionManager.Instance != null)UISelectionManager.Instance.ClearHighlight(true);
        OnPanelChanged?.Invoke(false);
    }
}
