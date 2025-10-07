using System;
using UnityEngine;
using UnityEngine.UI;

public class UISubControlPanel : MonoBehaviour {
    public bool IsOpen { get; private set; }
    [SerializeField] private GameObject[] inventoryTabs;
    [SerializeField] private GameObject _panelMain;
    [SerializeField] private Button[] inventoryTabButtons;
    [SerializeField] private UISubPanelOverview _panelOverviewScript;
    [SerializeField] private UISubPanelMove _panelMoveScript;
    public Transform PanelMain => _panelMain.transform;
    private int currentTabIndex;
    private UISubMovePopup _movePopup;
    private void Awake() {
        EnableTab(0);
        for (int i = 1; i < inventoryTabs.Length; i++) {
            if (inventoryTabs[i] != null) {
                inventoryTabs[i].SetActive(false);
            }
        }
    
        // Subscribe each tab button with its own captured index:
        for (int i = 0; i < inventoryTabButtons.Length; i++) {
            var button = inventoryTabButtons[i];
            int index = i;  // capture a fresh copy of i
            button.onClick.AddListener(() => OnTabButtonClicked(index));
        }
        _panelOverviewScript.InitParent(this);
        _panelMain.SetActive(false);
    }
    public void ControlPanelShow() {
        _panelMain.SetActive(true);
    }
    public void ControlPanelHide() {
        _panelMain.SetActive(false);
    }
    internal void ControlPanelToggle() {
        _panelMain.SetActive(!_panelMain.activeSelf);
    }
    public void OnTabButtonClicked(int i) {
        EnableTab(i);
        SetTabButtonVisual(i);
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
    internal void OnMovementRequestStart(bool isRequester, int zoneId, string message) {
        // Spawn popup if popup has not already been spawned, and we're not the requester

        Debug.Log($"MoveRequest started! isRequester: {isRequester}. ZoneID: {zoneId}");
        if (_movePopup == null && !isRequester) {
            _movePopup = Instantiate(App.ResourceSystem.GetPrefab<UISubMovePopup>("UIMovePopup"), _panelMain.transform);
            _movePopup.Init(zoneId);
        }
        // Enter the "move waiting" state in the movePanel for everyone
        _panelMoveScript.OnMoveEnter();
    }
    internal void OnMovementRequestUpdated(int requesterId, string requesterName, int[] acceptedIds, int[] pendingIds, string message) {
        // update the player statuses with the recieved Ids and stuff
        _panelMoveScript.UpdatePlayerStatus(acceptedIds, pendingIds);
    }

    internal void OnMovementRequestFailed(int requesterId, string requesterName, int[] acceptedIds, int[] pendingIds, string message) {
        // TODO!
        // Remove popup it its there
        if (_movePopup != null) {
            Destroy(_movePopup.gameObject); // Todo we'd probably want to say who denied
        }
        // Hide player status
        _panelMoveScript.OnMoveExit();
        Debug.LogWarning("Request failed!");
    }

    internal void OnMovementStarted(int requesterId, string requesterName, int[] acceptedIds, int[] pendingIds, string message) {
        // TODO some kind of screen shake + sound effect

        // Tell move panel to stop displaying status
        _panelMoveScript.OnMoveExit();
    }

    internal void OnNotifyActiveRequest(int requesterId, string requesterName, int[] acceptedIds, int[] pendingIds) {
        throw new NotImplementedException();
    }

    internal void OnRequestActionRejected(string message) {
        _panelMoveScript.RequestRejected();
    }

    
}