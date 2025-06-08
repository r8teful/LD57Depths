using FishNet.Object;
using System;
using UnityEngine;

public class PopupManager : MonoBehaviour {
    public static PopupManager Instance { get; private set; }
    public UIPopup popupPrefab;
    private UIPopup currentPopup;
    private IPopupInfo currentInfoProvider;
    private IPopupInfo currentHoveredInfoProvider;
    private IPopupInfo currentSelectedInfoProvider;
    private bool isMouseOverPopup;

    private void Awake() {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start() {
        GetComponent<InventoryUIManager>().OnInventoryToggle += OnInventoryToggled;
        //EventSystem.current.onSelectedGameObjectChanged.AddListener(OnSelectedGameObjectChanged);
    }
    private void OnDestroy() {
        GetComponent<InventoryUIManager>().OnInventoryToggle -= OnInventoryToggled;
    }

    private void OnInventoryToggled(bool isOpen) {
        Debug.Log("INVOKED!");
        if (!isOpen) {
            // Closing
            if(currentPopup != null) {
                HidePopup();
            }
        }
    }

    public void OnPointerEnterItem(IPopupInfo infoProvider) {
        currentHoveredInfoProvider = infoProvider;
        ShowPopup(infoProvider);
    }

    public void OnPointerExitItem() {
        if (!isMouseOverPopup) {
            currentHoveredInfoProvider = null;
            if (currentSelectedInfoProvider != null)
                ShowPopup(currentSelectedInfoProvider);
            else
                HidePopup();
        }
    }

    public void OnPointerEnterPopup() {
        isMouseOverPopup = true;
    }

    public void OnPointerExitPopup() {
        isMouseOverPopup = false;
        if (currentHoveredInfoProvider == null) {
            if (currentSelectedInfoProvider != null)
                ShowPopup(currentSelectedInfoProvider);
            else
                HidePopup();
        }
    }
    
    public void TryShowWorldPopup(IPopupInfo popupInfo, FishNet.Object.NetworkObject client) {
        if (true) {
            ShowPopup(popupInfo);
        }
    }
    private void OnSelectedGameObjectChanged(GameObject selected) {
        IPopupInfo newInfoProvider = selected?.GetComponent<IPopupInfo>();
        if (newInfoProvider != currentSelectedInfoProvider) {
            currentSelectedInfoProvider = newInfoProvider;
            if (currentHoveredInfoProvider == null && newInfoProvider != null)
                ShowPopup(newInfoProvider);
            else if (currentHoveredInfoProvider == null && newInfoProvider == null)
                HidePopup();
        }
    }

    private void ShowPopup(IPopupInfo infoProvider) {
        if (currentPopup != null && currentInfoProvider == infoProvider)
            return;

        HidePopup();
        currentInfoProvider = infoProvider;
        PopupData data = infoProvider.GetPopupData();
        infoProvider.PopupDataChanged += PopupDataChange;
        currentPopup = Instantiate(popupPrefab, transform);
        currentPopup.SetData(data);
        // Set up popup UI with data.title, data.description, data.additionalInfo
        PositionPopup(infoProvider);
    }

    private void PopupDataChange() {
        // Fetch new data
        currentPopup.SetData(currentInfoProvider.GetPopupData());
    }

    private void HidePopup() {
        if (currentPopup != null) {
            currentInfoProvider.PopupDataChanged -= PopupDataChange;
            Destroy(currentPopup.gameObject);
            currentPopup = null;
            currentInfoProvider = null;
        }
    }

    private void PositionPopup(IPopupInfo infoProvider) {
        RectTransform itemRT = (infoProvider as MonoBehaviour).GetComponent<RectTransform>();
        RectTransform popupRT = currentPopup.gameObject.GetComponent<RectTransform>();
        // Position popup relative to itemRT, adjust to stay within screen bounds
        Vector2 itemBottomCenter = new Vector2(itemRT.position.x, itemRT.position.y + itemRT.rect.yMin);
        Vector2 itemTopCenter = new Vector2(itemRT.position.x, itemRT.position.y + itemRT.rect.yMax);
        float popupHeight = popupRT.rect.height;

        // Try positioning below
        Vector2 position = itemBottomCenter;
        popupRT.position = new Vector3(position.x, position.y, 0);

        // Check if bottom is off-screen
        float popupBottom = position.y + popupRT.rect.yMin;
        if (popupBottom < 0) {
            // Position above
            position = new Vector2(itemTopCenter.x, itemTopCenter.y + popupHeight);
            popupRT.position = new Vector3(position.x, position.y, 0);
        }
    }
}