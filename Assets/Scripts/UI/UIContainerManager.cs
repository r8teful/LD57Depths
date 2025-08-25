using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

public class UIContainerManager : MonoBehaviour {
    public GameObject slotPrefab;
    public Transform slotParent;
    public GameObject containerPanel;
    public Button closeButton; // Optional: A dedicated button to close the container UI

    private List<UIInventoryItem> uiSlots = new List<UIInventoryItem>();
    private SharedContainer _currentContainer;
    private NetworkedPlayerInventory _localPlayerInventory;

    void Start() {
        // Assuming only one local player inventory.
        _localPlayerInventory = FindObjectsOfType<NetworkedPlayerInventory>().FirstOrDefault(inv => inv.IsOwner);
        if (containerPanel != null)
            containerPanel.SetActive(false);
        if (closeButton != null)
            closeButton.onClick.AddListener(HandleCloseButtonClick);
    }

    private void OnDestroy() {
        if (_currentContainer != null) {
            _currentContainer.OnContainerInventoryChanged -= RefreshUIContents;
            _currentContainer.OnLocalPlayerInteractionStateChanged -= HandleInteractionStateChanged;
        }
        if (closeButton != null)
            closeButton.onClick.RemoveListener(HandleCloseButtonClick);
    }

    public void OpenContainerUI(SharedContainer container) {
        if (_localPlayerInventory == null) {
            Debug.LogError("Cannot open container UI: LocalPlayerInventory not found.");
            return;
        }
        if (_currentContainer != null && _currentContainer != container) {
            CloseCurrentContainerUI(); // Close previous if opening a new one
        }


        _currentContainer = container;
        _currentContainer.OnContainerInventoryChanged += RefreshUIContents;
        _currentContainer.OnLocalPlayerInteractionStateChanged += HandleInteractionStateChanged;

        // Request server to open. Server will respond, and OnLocalPlayerInteractionStateChanged will show UI.
        _currentContainer.CmdRequestOpenContainer();
    }

    private void HandleInteractionStateChanged(bool isOpenForThisPlayer, List<InventorySlot> initialSlots) {
        if (isOpenForThisPlayer) {
            if (containerPanel == null)
                return;
            containerPanel.SetActive(true);
            //PopulateSlots(initialSlots ?? new List<InventorySlot>(_currentContainer.ContainerSlots)); // Use initial if provided
        } else {
            CloseCurrentContainerUI(false); // Don't send close command again if server initiated closure
        }
    }

    public void RefreshUIContents() {
        if (_currentContainer == null || containerPanel == null || !containerPanel.activeSelf)
            return;

        // This ensures the UI matches the SyncList from the container
        // If counts mismatch, repopulate. This can happen if containerSize changes dynamically (rare).
        if (uiSlots.Count != _currentContainer.ContainerSlots.Count) {
            //PopulateSlots(new List<InventorySlot>(_currentContainer.ContainerSlots));
        } else {
            for (int i = 0; i < _currentContainer.ContainerSlots.Count; i++) {
                if (i < uiSlots.Count)
                    uiSlots[i].UpdateSlot(_currentContainer.ContainerSlots[i]);
            }
        }
    }


    // Called by InventoryUISlot when a slot IN THIS CONTAINER UI is clicked
    public void ContainerSlotClicked(int slotIndex, int mouseButton) // 0 Left, 1 Right
    {
        if (_currentContainer == null || _localPlayerInventory == null || !_localPlayerInventory.IsOwner)
            return;
        if (slotIndex < 0 || slotIndex >= _currentContainer.ContainerSlots.Count)
            return;

        InventorySlot clickedSlotInContainer = _currentContainer.ContainerSlots[slotIndex]; // Get live data
    }

    private void HandleCloseButtonClick() {
        CloseCurrentContainerUI(true); // Send close command to server
    }

    public void CloseCurrentContainerUI(bool sendServerCommand = true) {
        if (containerPanel != null)
            containerPanel.SetActive(false);

        if (_currentContainer != null) {
            
        }
        uiSlots.Clear(); // Clean up UI elements if needed, or just hide parent
    }
}