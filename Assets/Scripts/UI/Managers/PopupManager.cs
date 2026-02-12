using System;
using UnityEngine;
using UnityEngine.UI;

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
        GetComponent<UIManager>().UpgradeScreen.OnPanelChanged += TryHidePopup;
        //EventSystem.current.onSelectedGameObjectChanged.AddListener(OnSelectedGameObjectChanged);
    }

    private void TryHidePopup(bool isActive) {
        if (!isActive && currentPopup != null) {
            HidePopup();
        }
    }

    private void OnDestroy() {
        GetComponent<UIManager>().UpgradeScreen.OnPanelChanged -= TryHidePopup;
    }

    private void OnInventoryToggled(bool isOpen) {
        //Debug.Log("INVOKED!");
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
        //LayoutRebuilder.ForceRebuildLayoutImmediate(currentPopup.GetComponent<RectTransform>());
        PositionPopup(infoProvider);
        currentPopup.ShowAnimate();
    }

    private void PopupDataChange() {
        // Fetch new data
        currentPopup.SetData(currentInfoProvider.GetPopupData(inventoryManager));
    }

    private void HidePopup() {
        if (currentPopup != null) {
            currentInfoProvider.PopupDataChanged -= PopupDataChange;
            Destroy(currentPopup.gameObject); // This is breaking the UINODETWEEN!?!?
            currentPopup = null;
            currentInfoProvider = null;
        }
    }

   
    private void PositionPopup(IPopupInfo infoProvider) {
        var mb = infoProvider as MonoBehaviour;
        if (mb == null) return;
        RectTransform itemRT = mb.GetComponent<RectTransform>();
        RectTransform popupRT = currentPopup.gameObject.GetComponent<RectTransform>();
        if (itemRT == null || popupRT == null) return;

        // Canvas (ScreenSpaceOverlay -> canvas.worldCamera is null)
        Canvas canvas = popupRT.GetComponentInParent<Canvas>();
        RectTransform popupParentRect = popupRT.parent as RectTransform;
        Camera canvasCam = null; // overlay uses null camera in RectTransformUtility

        // Ensure layout is updated so popupRT.rect is valid
        LayoutRebuilder.ForceRebuildLayoutImmediate(popupRT);
        // Get item bottom-center and top-center in screen space
        Vector3[] itemCorners = new Vector3[4];
        itemRT.GetWorldCorners(itemCorners);
        Vector2 itemBottomScreen = RectTransformUtility.WorldToScreenPoint(canvasCam, (itemCorners[0] + itemCorners[3]) * 0.5f);
        Vector2 itemTopScreen = RectTransformUtility.WorldToScreenPoint(canvasCam, (itemCorners[1] + itemCorners[2]) * 0.5f);
        

        // Popup size in screen pixels (for ScreenSpaceOverlay this is reliable)
        float scaleFactor = (canvas != null) ? canvas.scaleFactor : 1f;
        float popupScreenH = popupRT.rect.height * scaleFactor;
        float popupScreenW = popupRT.rect.width * scaleFactor;

        float paddingPx = 6f;

        // Compute fits using the *assumed* pivot for each placement:
        // - placing below => popup pivot.y will be 1 (top of popup attaches to item bottom)
        //   bottom of popup in screen = itemBottomScreen.y - popupScreenH
        // - placing above => popup pivot.y will be 0 (bottom of popup attaches to item top)
        //   top of popup in screen = itemTopScreen.y + popupScreenH

        bool fitsBelow = (itemBottomScreen.y - popupScreenH - paddingPx) >= 0f;
        bool fitsAbove = (itemTopScreen.y + popupScreenH + paddingPx) <= Screen.height;

        bool placeBelow;
        if (fitsBelow) placeBelow = true;
        else if (fitsAbove) placeBelow = false;
        else {
            // choose the side with more available space
            float spaceBelow = itemBottomScreen.y;
            float spaceAbove = Screen.height - itemTopScreen.y;
            placeBelow = spaceBelow >= spaceAbove;
        }

        // Set pivot.y to match the chosen placement (leave pivot.x alone to respect your horizontal anchoring)
        Vector2 origPivot = popupRT.pivot;
        popupRT.pivot = new Vector2(origPivot.x, placeBelow ? 1f : 0f);

        // Build the anchor screen point where popup pivot should be located
        Vector2 anchorScreenPoint = placeBelow
            ? new Vector2(itemBottomScreen.x, itemBottomScreen.y - paddingPx)
            : new Vector2(itemTopScreen.x, itemTopScreen.y + paddingPx);

        // Convert that screen point into a world point on the popup parent's plane and set popup position
        Vector3 worldPos;
        if (popupParentRect != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(popupParentRect, anchorScreenPoint, canvasCam, out worldPos)) {
            popupRT.position = worldPos;
        } else {
            // fallback: use canvas rect plane
            RectTransform canvasRect = (canvas != null) ? (canvas.transform as RectTransform) : popupRT;
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, anchorScreenPoint, canvasCam, out worldPos))
                popupRT.position = worldPos;
        }

        // Horizontal clamp: nudge anchorScreenPoint.x if popup overflows left/right
        // Check popup world corners -> screen x positions
        Vector3[] popupWorldCorners = new Vector3[4];
        popupRT.GetWorldCorners(popupWorldCorners);
        float leftScreen = RectTransformUtility.WorldToScreenPoint(canvasCam, popupWorldCorners[0]).x;
        float rightScreen = RectTransformUtility.WorldToScreenPoint(canvasCam, popupWorldCorners[2]).x;

        float shiftPx = 0f;
        if (leftScreen < 0f) shiftPx = -leftScreen;
        else if (rightScreen > Screen.width) shiftPx = Screen.width - rightScreen;

        if (!Mathf.Approximately(shiftPx, 0f)) {
            anchorScreenPoint.x += shiftPx;
            if (popupParentRect != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(popupParentRect, anchorScreenPoint, canvasCam, out worldPos)) {
                popupRT.position = worldPos;
            } else {
                RectTransform canvasRect = (canvas != null) ? (canvas.transform as RectTransform) : popupRT;
                if (RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, anchorScreenPoint, canvasCam, out worldPos))
                    popupRT.position = worldPos;
            }
        }
    }
    public void ShowPopup(IPopupInfo p, bool b) {
        OnPopupShow(p, b);
    }
    private void OnPopupShow(IPopupInfo popup, bool shouldShow) {
        if (shouldShow) {
            OnPointerEnterItem(popup);
        } else {
            OnPointerExitItem();
        }
    }
}