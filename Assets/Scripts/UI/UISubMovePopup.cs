using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISubMovePopup : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI _zoneText;
    [SerializeField] private Transform _zoneResourcesContainer;
    [SerializeField] private Button _buttonConfirm;
    [SerializeField] private Button _buttonDeny;
    [SerializeField] private UISubMap _mapScript;

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
