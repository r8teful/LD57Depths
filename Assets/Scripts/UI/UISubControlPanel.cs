using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISubControlPanel : MonoBehaviour {
    public bool IsOpen { get; private set; }

    [SerializeField] private GameObject[] inventoryTabs;
    [SerializeField] private Transform mapButtonsContainer;
    [SerializeField] private Transform zoneResourcesContainer;
    [SerializeField] private TextMeshProUGUI _zoneText;
    private Button[] mapButtons;
    [SerializeField] private Button[] inventoryTabButtons;
    private int currentTabIndex;
    private void Awake() {
        EnableTab(0);
        for (int i = 1; i < inventoryTabs.Length; i++) {
            if (inventoryTabs[i] != null) {
                inventoryTabs[i].SetActive(false);
            }
        }
        mapButtons = mapButtonsContainer.GetComponentsInChildren<Button>()
            .OrderBy(b => b.transform.GetSiblingIndex())
            .ToArray();
        for (int i = 0; i < mapButtons.Length; i++) {
            var button = mapButtons[i];
            int index = i;  // capture a fresh copy of i
            button.onClick.AddListener(() => OnMapButtonClicked(index));
        }
        // Subscribe each tab button with its own captured index:
        for (int i = 0; i < inventoryTabButtons.Length; i++) {
            var button = inventoryTabButtons[i];
            int index = i;  // capture a fresh copy of i
            button.onClick.AddListener(() => OnTabButtonClicked(index));
        }
    }
    private void Start() {
        SetMapButtonVisual(0); // TODO should be current area
    }
    public void OnTabButtonClicked(int i) {
        EnableTab(i);
        SetTabButtonVisual(i);
    }
    public void OnMapButtonClicked(int i) {
        SetMapInfo(i);
        SetMapButtonVisual(i);
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
    private void SetMapButtonVisual(int i,bool setActive) {
        var image = mapButtons[i].GetComponent<Image>();
        if (setActive) { 
            image.color = Color.white;
            mapButtons[i].transform.SetAsLastSibling();
        } else {
            image.color = Color.red;
            if (ColorUtility.TryParseHtmlString("#5BE5C0", out var color)){
                image.color = color;
            }
        }
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

    private void EnableTab(int i) {
        if (inventoryTabs == null || inventoryTabs.Length == 0) {
            Debug.LogWarning("inventoryTabs array is null or empty!");
            return;
        }

        if (i < 0 || i >= inventoryTabs.Length) {
            Debug.LogWarning($"Tab index {i} is out of range. Valid range: 0 to {inventoryTabs.Length - 1}");
            return;
        }
        currentTabIndex = i;
        for (int j = 0; j < inventoryTabs.Length; j++) {
            if (inventoryTabs[j] != null) {
                inventoryTabs[j].SetActive(j == i);
            } else {
                Debug.LogWarning($"Tab at index {j} is null.");
            }
        }
    }
    private void SetTabButtonVisual(int i) {
        if (inventoryTabButtons == null || inventoryTabButtons.Length == 0) {
            Debug.LogWarning("inventoryTabs array is null or empty!");
            return;
        }
        if (i < 0 || i >= inventoryTabButtons.Length) {
            Debug.LogWarning($"Tab index {i} is out of range. Valid range: 0 to {inventoryTabButtons.Length - 1}");
            return;
        }
        for (int j = 0; j < inventoryTabButtons.Length; j++) {
            if (inventoryTabButtons[j] != null) {
                SetTabButtonVisual(j, j == i);
            } else {
                Debug.LogWarning($"Tab at index {j} is null.");
            }
        }
    }
    private void SetTabButtonVisual(int i, bool setActive) {
        var button = inventoryTabButtons[i];
        if (button != null) {
            button.GetComponent<UITabButton>().SetButtonVisual(setActive);
        }
    }
    // direction should be either -1 or 1 idealy
    public void HandleScrollTabs(int direction) {
        if (!IsOpen)
            return; // Don't scroll if inventory is not open
        if (inventoryTabs == null || inventoryTabs.Length == 0) {
            Debug.LogWarning("inventoryTabs array is null or empty!");
            return;
        }

        int newIndex = (currentTabIndex + direction + inventoryTabs.Length) % inventoryTabs.Length;

        // Clamp the value to valid range
        //newIndex = Mathf.Clamp(newIndex, 0, inventoryTabs.Length - 1);

        if (newIndex != currentTabIndex) {
            OnTabButtonClicked(newIndex); // Handle it as a click
        }
    }

    // TODO
    // Set the color of the selected Map layer to white, then also set it as the last sibling so it is at the top, this will make it look nice
    // Show the details of that zone
    // Keep track of the selected one, 
}
