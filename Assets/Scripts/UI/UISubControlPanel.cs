using System;
using UnityEngine;
using UnityEngine.UI;

public class UISubControlPanel : MonoBehaviour {
    public bool IsOpen { get; private set; }
    [SerializeField] private GameObject[] inventoryTabs;
    [SerializeField] private Button[] inventoryTabButtons;
    [SerializeField] private UISubPanelOverview _overviewScript;
    private int currentTabIndex;
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
        _overviewScript.InitParent(this);
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

    internal void ShowMovePopup(int playerId, int zoneID) {
        Instantiate(App.ResourceSystem.GetPrefab<UISubMovePopup>("UIMovePopup"),transform).Init(zoneID);
        
    }

    internal void OnMovementRequestUpdated(int requesterId, string requesterName, int[] acceptedIds, int[] pendingIds, string message) {
        // If its our first time, spawn popup.
        // If popup has already been spawned, or we're the requester, update the player statuses with the recieved Ids and stuff
    }
    internal void OnMovementRequestFailed(int requesterId, string requesterName, int[] acceptedIds, int[] pendingIds, string message) {
        throw new NotImplementedException();
    }

    internal void OnMovementStarted(int requesterId, string requesterName, int[] acceptedIds, int[] pendingIds, string message) {
        // TODO some kind of screen shake + sound effect
    }

    internal void OnNotifyActiveRequest(int requesterId, string requesterName, int[] acceptedIds, int[] pendingIds) {
        throw new NotImplementedException();
    }

    internal void OnRequestActionRejected(string message) {
        throw new NotImplementedException();
    }
}