using DG.Tweening;
using TMPro; 
using UnityEngine;
using UnityEngine.UI;

public class UIInventoryItem : MonoBehaviour {
    [SerializeField] private Image itemIconImage; 
    [SerializeField] private TextMeshProUGUI quantityText; 
  
    void Awake() {
        if (!itemIconImage || !quantityText) {
            Debug.LogError($"Slot UI on {gameObject.name} is missing references to Icon Image or Quantity Text!", gameObject);
        }
    }
    private void OnDestroy() {
    }
    public void Remove() {
        transform.DOKill();
        quantityText.transform.DOKill();
        Destroy(gameObject);
    }
    public void Init(ItemData item) {
        itemIconImage.sprite = item.icon;

        // pop in
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
    }

    public void UpdateSlot(int amount) {
        quantityText.text = amount.ToString();
        DoAnimation(); // Satisfying!
    }

    private void DoAnimation() {
        quantityText.transform.localScale = Vector3.one;
        quantityText.transform.DOKill();
        var punchAmount = 1.0001f;
        quantityText.transform.DOPunchScale(new Vector3(punchAmount, punchAmount, punchAmount), 0.3f, 1, 1f);

    }
}