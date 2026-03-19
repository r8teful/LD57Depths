using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISubControlPanel : MonoBehaviour {
    private int _currentShownIndex;
    private int _currentSubIndex;
    public bool IsOpen { get; private set; }
    [SerializeField] private GameObject _panelMain;
    public Transform PanelMain => _panelMain.transform;

    [SerializeField] private TextMeshProUGUI _zoneText;
    [SerializeField] private TextMeshProUGUI _needToUpgradeText;
    [SerializeField] private Transform _zoneResourcesContainerGrid;
    [SerializeField] private RectTransform _panelMainRect;
    [SerializeField] private GameObject _resourceParent;
    [SerializeField] private Button _buttonMove;

    [SerializeField] private UISubMap _mapScript;
    [SerializeField] private UISubmarineOverview _subOverviewScript;
    private void Awake() {
        _panelMain.SetActive(false);
        _buttonMove.onClick.AddListener(OnMoveClicked);
        _mapScript.Init(this);
        SubmarineManager.Instance.OnSubMoved += SubMoved;
    }
    private void OnDestroy() {
        if(SubmarineManager.Instance != null){
            SubmarineManager.Instance.OnSubMoved -= SubMoved;

        }
    }
    private void SubMoved(int currentMovedIndex) {
        // Update to new zone
        _currentSubIndex = currentMovedIndex;
        ChangeSubPanel(_currentShownIndex);
    }
    public void ControlPanelShow() {
        ChangeSubPanel(SubmarineManager.Instance.CurrentZoneIndex);
        _panelMain.SetActive(true);
        // we're now open
        _panelMain.transform.localScale = new(1, 0.2f, 1);
        // _upgradePanelRect.DOScaleY(1, 0.6f).SetEase(Ease.OutElastic);
        _panelMainRect.DOScaleY(1, 0.2f).SetEase(Ease.OutBack);
    }
    public void ControlPanelHide() {
        _panelMainRect.localScale = Vector3.one;
        _panelMainRect.DOScaleY(0.2f, 0.05f).SetEase(Ease.OutCubic).
            OnComplete(() => {
                _panelMain.SetActive(false);
            });
    }
    internal void ControlPanelToggle() {
        if (_panelMain.activeSelf) {
            // hide
            ControlPanelHide();
        } else {
            ControlPanelShow();
        }
    }

    private void OnEnable() {
        // Start showing our current zone
        ChangeSubPanel(SubmarineManager.Instance.CurrentZoneIndex);
    }
   
    public void OnMapButtonClicked(ZoneSO zoneinfo) {
        ChangeSubPanel(zoneinfo);
    }
    private void ChangeSubPanel(int zoneIndex) {
        var zoneData = App.ResourceSystem.GetZoneByIndex(zoneIndex);
        ChangeSubPanel(zoneData);
    }
    private void ChangeSubPanel(ZoneSO zone) {
        _currentShownIndex = zone.ZoneIndex;
        _zoneText.text = zone.GetLocalizedZoneName();
        bool canMoveToZone = SubmarineManager.Instance.CanMoveTo(_currentShownIndex);
        
        SetButtonAndText(zone, canMoveToZone);
        SetZoneResources(zone,canMoveToZone);
        UpdateSubVisual(zone, canMoveToZone);
    }

    private void SetButtonAndText(ZoneSO zone, bool canMoveToZone) {
        // Should move button be visable?
        if (!canMoveToZone) {
            // show text, hide button.
            _needToUpgradeText.gameObject.SetActive(true);
            _needToUpgradeText.text = zone.GetLocalizedZoneCantGetDesc();
            _buttonMove.gameObject.SetActive(false);

        } else {
            _needToUpgradeText.gameObject.SetActive(false);
            _buttonMove.interactable = true;
            _buttonMove.gameObject.SetActive(true);
        }
    }

    private void UpdateSubVisual(ZoneSO zone, bool canMoveToZone) {
        if (!canMoveToZone) {
            _subOverviewScript.gameObject.SetActive(true);
            _subOverviewScript.SetIndex(zone.ZoneIndex);
            return;
        }
        _subOverviewScript.gameObject.SetActive(false);
    }
    private void SetZoneResources(ZoneSO zone, bool canMoveToZone) {
        // Clear all first
        for (int j = 0; j < _zoneResourcesContainerGrid.childCount; j++) {
            Destroy(_zoneResourcesContainerGrid.GetChild(j).gameObject);
        }
        if (!canMoveToZone) {
            // Don't show resources when we can't go there
            _resourceParent.SetActive(false);
            return;
        }
        _resourceParent.SetActive(true);
        // Now polulate
        foreach (var item in zone.AvailableResources) {
            var g = Instantiate(App.ResourceSystem.GetPrefab("UIZoneResourceElement"), _zoneResourcesContainerGrid); // Will automatically make a nice grid
            g.GetComponent<Image>().sprite = item.icon;
            
        }
    }

    private void OnMoveClicked() {
        // Server handles most logic, will call OnMoveEnter if succefull 
        SubmarineManager.Instance.MoveSub(_currentShownIndex);
    }
}