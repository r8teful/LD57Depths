using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIItemQuantity : MonoBehaviour {
    [SerializeField] private Image _itemIcon;
    [SerializeField] private TextMeshProUGUI _itemQuanity;
    internal void Init(ItemQuantity item) {
        _itemIcon.sprite = item.item.icon;
        _itemQuanity.text = item.quantity.ToString();
    }
}