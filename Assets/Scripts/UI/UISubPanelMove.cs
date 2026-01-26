using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISubPanelMove : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI _zoneText;
    [SerializeField] private Transform _zoneResourcesContainer;
    [SerializeField] private Transform _playerStatusContainer;
    [SerializeField] private GameObject _waitingContainer;
    [SerializeField] private Button _buttonMove;
    [SerializeField] private TextMeshProUGUI _buttonMoveText;

    [SerializeField] private UISubMap _mapScript;
    private int _currentShownIndex;
    private void Awake() {
        _buttonMove.onClick.AddListener(OnMoveClicked);
        _mapScript.Init(this);
        _waitingContainer.SetActive(false);
        SubmarineManager.Instance.OnSubMoved += SubMoved;
    }

    private void Start() {
        _currentShownIndex = SubmarineManager.Instance.CurrentZoneIndex;
        UpdateMoveButton();
    }
    private void OnDestroy() {
        SubmarineManager.Instance.OnSubMoved -= SubMoved;
    }    
    public void OnMapButtonClicked(ZoneSO zoneinfo) {
        _currentShownIndex = zoneinfo.ZoneIndex;
        SetMapInfo(zoneinfo);
    }
    private void SetMapInfo(ZoneSO zone) {
        _zoneText.text = zone.ZoneName;

        // Resources

        // Clear all first
        for (int j = 0; j < _zoneResourcesContainer.childCount; j++) {
            Destroy(_zoneResourcesContainer.GetChild(j).gameObject);
        }
        // Now polulate
        foreach (var item in zone.AvailableResources) {
            var g = Instantiate(App.ResourceSystem.GetPrefab("UIZoneResourceElement"), _zoneResourcesContainer); // Will automatically make a nice grid
            if (DiscoveryManager.Instance.IsDiscovered(item)) {
                g.GetComponent<Image>().sprite = item.icon;
            } else {
                g.GetComponent<Image>().sprite = App.ResourceSystem.GetSprite("ItemUnknown");
            }
        }
        UpdateMoveButton(); 
    }
    private void UpdateMoveButton() {
        // Should move button be visable?
        _buttonMove.interactable = true;
        if (_currentShownIndex == SubmarineManager.Instance.CurrentZoneIndex) {
            // hide
            _buttonMove.gameObject.SetActive(false);
        } else if (!_buttonMove.gameObject.activeSelf) {
            _buttonMove.gameObject.SetActive(true);
        }

        // Text
        if (SubmarineManager.Instance.CanMoveTo(_currentShownIndex)) {
            _buttonMoveText.text = "MOVE";
        } else {
            _buttonMoveText.text = "UPGRADE SHIP";
            _buttonMove.interactable = false;
        }
    }
 

    private void OnMoveClicked() {
        // Server handles most logic, will call OnMoveEnter if succefull 
        SubmarineManager.Instance.MoveSub(_currentShownIndex);
    }
    private void SubMoved() {
        // Update to new zone
        UpdateMoveButton();
    }

    public void OnMoveExit() {
        //_buttonMove.gameObject.SetActive(true);
        _waitingContainer.SetActive(false);
        _mapScript.EnableMapInteractions();
    }
}