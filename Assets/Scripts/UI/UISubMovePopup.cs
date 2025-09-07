using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISubMovePopup : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI _zoneText;
    [SerializeField] private Transform _zoneResourcesContainer;
    [SerializeField] private Button _buttonConfirm;
    [SerializeField] private Button _buttonDeny;
    [SerializeField] private UISubMap _mapScript;
    private void Awake() {
        _buttonConfirm.onClick.AddListener(OnConfirmClicked);
        _buttonConfirm.onClick.AddListener(OnDenyClicked);
    }
    private void OnDestroy() {
        _buttonConfirm.onClick.RemoveListener(OnConfirmClicked);
        _buttonConfirm.onClick.RemoveListener(OnDenyClicked);
    }

    private void OnDenyClicked() {
        Destroy(gameObject);
    }

    private void OnConfirmClicked() {
        // TODO here we need to make sure we can see the status of the other players
        
        // Send server message that we've clicked

        // Server then sends observer message to each MovementStatus or something
        // Movement status checks if the playerID of the one that clicked confirm matches the cached one
        // If it does it goes green,

        // OR, when we confirm, we add that ID to the server sided "player conformied" list,
        // MovementStatus is subscribed to the onchange, then updates accordingly

        Destroy(gameObject);
    }

    public void Init(int zoneId) {
        ZoneSO zone = App.ResourceSystem.GetZoneByIndex(zoneId);
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
        _mapScript.SetBoxHighlight(zone);
    }

}
