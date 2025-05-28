using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class UIPopup : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler {
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Transform ingredientsContent; // VerticalLayoutGroup
    public UIIngredientVisual ingredientPrefab;
    public ItemData itemData;
    public void OnPointerEnter(PointerEventData eventData) {
        PopupManager.Instance.OnPointerEnterPopup();
    }

    public void OnPointerExit(PointerEventData eventData) {
        PopupManager.Instance.OnPointerExitPopup();
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
        } else {
            ingredientsContent.gameObject.SetActive(false);
        }
    }
}