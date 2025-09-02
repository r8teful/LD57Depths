using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISubPanelMove : MonoBehaviour {
    [SerializeField] private Transform mapButtonsContainer;
    private Button[] mapButtons;
    [SerializeField] private TextMeshProUGUI _zoneText;
    [SerializeField] private Transform zoneResourcesContainer;
    private void Awake() {
        mapButtons = mapButtonsContainer.GetComponentsInChildren<Button>()
        .OrderBy(b => b.transform.GetSiblingIndex())
        .ToArray();

        for (int i = 0; i < mapButtons.Length; i++) {
            var button = mapButtons[i];
            int index = i;  // capture a fresh copy of i
            button.onClick.AddListener(() => OnMapButtonClicked(index));
        }
    }
    private void Start() {
        SetMapButtonVisual(0); // TODO should be current area
    }
    public void OnMapButtonClicked(int i) {
        SetMapInfo(i);
        SetMapButtonVisual(i);
    }
    private void SetMapInfo(int i) {
        var zone = mapButtons[i].GetComponent<TrenchZone>();
        if (zone == null) {
            Debug.LogError("Could not find trenchZone attached to button");
        }
        _zoneText.text = zone.ZoneData.ZoneName;

        // Resources

        // Clear all first
        for (int j = 0; j < zoneResourcesContainer.childCount; j++) {
            Destroy(zoneResourcesContainer.GetChild(j).gameObject);
        }
        // Now polulate
        foreach (var item in zone.ZoneData.AvailableResources) {
            var g = Instantiate(App.ResourceSystem.GetPrefab("UIZoneResourceElement"), zoneResourcesContainer); // Will automatically make a nice grid
            g.GetComponent<Image>().sprite = item.icon;
        }
    }
    private void SetMapButtonVisual(int i) {
        if (mapButtons == null || mapButtons.Length == 0) {
            Debug.LogWarning("inventoryTabs array is null or empty!");
            return;
        }
        if (i < 0 || i >= mapButtons.Length) {
            Debug.LogWarning($"Tab index {i} is out of range. Valid range: 0 to {mapButtons.Length - 1}");
            return;
        }
        for (int j = 0; j < mapButtons.Length; j++) {
            if (mapButtons[j] != null) {
                SetMapButtonVisual(j, j == i);
            } else {
                Debug.LogWarning($"Tab at index {j} is null.");
            }
        }
    }
    private void SetMapButtonVisual(int i, bool setActive) {
        var image = mapButtons[i].GetComponent<Image>();
        if (setActive) {
            image.color = Color.white;
            mapButtons[i].transform.SetAsLastSibling();
        } else {
            image.color = Color.red;
            if (ColorUtility.TryParseHtmlString("#5BE5C0", out var color)) {
                image.color = color;
            }
        }
    }

}
