using System;
using UnityEngine;

public class PopupManager : StaticInstance<PopupManager> {
    public UIPopup popupPrefab;
    private UIPopup currentPopup;
    private IPopupInfo currentInfoProvider;
    private IPopupInfo currentHoveredInfoProvider;
    private IPopupInfo currentSelectedInfoProvider;
    private bool isMouseOverPopup;
    private InventoryManager inventoryManager;
    public UIPopup CurrentPopup => currentPopup;
    //private void Awake() {
    //    if (Instance == null)
    //        Instance = this;
    //    else
    //        Destroy(gameObject);
    //}
    public void Init(InventoryManager clientInv) {
        inventoryManager = clientInv; // need it for popup box info
    }
    private void Start() {
        GetComponent<UIManager>().UIManagerInventory.OnInventoryToggle += OnInventoryToggled;
        //EventSystem.current.onSelectedGameObjectChanged.AddListener(OnSelectedGameObjectChanged);
    }
    private void OnDestroy() {
        GetComponent<UIManager>().UIManagerInventory.OnInventoryToggle -= OnInventoryToggled;
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
        Debug.Log("EXitPopup!");
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
        PopupData data = infoProvider.GetPopupData(inventoryManager);
        infoProvider.PopupDataChanged += PopupDataChange;
        currentPopup = Instantiate(popupPrefab, transform);
        currentPopup.SetData(data);
        // Set up popup UI with data.title, data.description, data.additionalInfo
        PositionPopup(infoProvider);
    }

    private void PopupDataChange() {
        // Fetch new data
        currentPopup.SetData(currentInfoProvider.GetPopupData(inventoryManager));
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

        // Calculate item bottom and top centers
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

        // Clamp x to stay within screen horizontally
        float leftBound = -popupRT.rect.xMin;
        float rightBound = Screen.width - popupRT.rect.xMax;
        float clampedX = Mathf.Clamp(popupRT.position.x, leftBound, rightBound);
        popupRT.position = new Vector3(clampedX, popupRT.position.y, 0);
    }

    internal void RegisterIPopupInfo(IPopupInfo popupInfo) {
        popupInfo.OnPopupShow += OnPopupShow;
    }
    // We really should unsuscribe here 
    public void UnregisterIPopupInfo(IPopupInfo popupInfo) {
        popupInfo.OnPopupShow -= OnPopupShow;
    }

    private void OnPopupShow(IPopupInfo popup, bool shouldShow) {
        if (shouldShow) {
            OnPointerEnterItem(popup);
        } else {
            OnPointerExitItem();
        }
    }
}