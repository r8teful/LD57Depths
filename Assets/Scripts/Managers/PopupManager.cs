using System;
using UnityEngine;

public class PopupManager : MonoBehaviour {
    public static PopupManager Instance { get; private set; }
    public UIPopup popupPrefab;
    private GameObject currentPopup;
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
        //EventSystem.current.onSelectedGameObjectChanged.AddListener(OnSelectedGameObjectChanged);
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
        var p = Instantiate(popupPrefab, transform);
        p.SetData(data);
        currentPopup = p.gameObject;
        // Set up popup UI with data.title, data.description, data.additionalInfo
        PositionPopup(infoProvider);
    }

    private void HidePopup() {
        if (currentPopup != null) {
            Destroy(currentPopup);
            currentPopup = null;
            currentInfoProvider = null;
        }
    }

    private void PositionPopup(IPopupInfo infoProvider) {
        RectTransform itemRT = (infoProvider as MonoBehaviour).GetComponent<RectTransform>();
        RectTransform popupRT = currentPopup.GetComponent<RectTransform>();
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