using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// Should be generic enough to display any kind of data that popups up on the screen, either in world space, or on the canvas, for example next to the cursor 
public class UIPopup : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    [SerializeField] private GameObject popupBackground;
    [SerializeField] private RectTransform rectTransform;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Transform ingredientsContent; // VerticalLayoutGroup
    public UIIngredientVisual ingredientPrefab;
    public ItemData itemData;
    private bool _isWorldPopup; // Is this popup on a world space canvas?
    private void Start() {
        // Try and find the canvas lol
        Canvas c0 = null;
        Canvas c1 = null;
        c0 = GetComponentInParent<Canvas>();
        var t = transform.parent;
        if (t != null) {
            c1 = t.GetComponentInParent<Canvas>();
        }
        if (c0 != null) {
            _isWorldPopup = c0.renderMode == RenderMode.WorldSpace;
        }  else if (c1 != null){
            _isWorldPopup = c0.renderMode == RenderMode.WorldSpace;
        }
    }
    public void OnPointerEnter(PointerEventData eventData) {
       // PopupManager.Instance.OnPointerEnterPopup();
    }

    public void OnPointerExit(PointerEventData eventData) {
        //PopupManager.Instance.OnPointerExitPopup();
    }
    public void SetData(PopupData data) {
        nameText.text = data.title;
        descriptionText.text = data.description;
        if (data.craftingInfo != null && data.craftingInfo.Count > 0) {
            ingredientsContent.gameObject.SetActive(true);
            foreach (Transform child in ingredientsContent) {
                Destroy(child.gameObject);
            }
            foreach (var ingredient in data.craftingInfo) {
                Instantiate(ingredientPrefab, ingredientsContent).Init(ingredient);
            }
            rectTransform.ForceUpdateRectTransforms();
            //var rect = Instantiate(popupBackground, transform.parent).GetComponent<RectTransform>().sizeDelta;
            //rect.y = rectTransform.sizeDelta.y;
            //rectTransform.SetAsLastSibling();
            StartCoroutine(SetBackgroundSize());
        } else {
            ingredientsContent.gameObject.SetActive(false);
        }
    }

    // This has to be a stupid ienumerator because unity is stupid and annoying 
    private IEnumerator SetBackgroundSize() {
        while (true) {

        var newHeight = transform.GetComponent<RectTransform>().sizeDelta.y;
        var rect = transform.GetChild(0).GetComponent<RectTransform>();
        // Anchored at the top, so offsetMax.y stays the same (usually 0)
        // We set offsetMin.y to -height to make it the correct height
        Vector2 offsetMin = rect.offsetMin;
        offsetMin.y = -newHeight;
        rect.offsetMin = offsetMin;

        Vector2 offsetMax = rect.offsetMax;
        offsetMax.y = 0;
        rect.offsetMax = offsetMax;
        yield return new WaitForEndOfFrame();
        }
    }
    public void HandleFailVisual() {
        if (_isWorldPopup) {
            transform.DOShakePosition(0.2f,0.3f,50);
        } else {
            transform.DOShakePosition(0.2f,15f,30);
        }
    }
}