using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIResourceElement : MonoBehaviour {
    public Image resourceIcon;
    public TextMeshProUGUI resourceAmountText;
    public ItemData ResourceType;
    public void Init(ItemData type, int amount) {
        ResourceType = type;
        Sprite sprite = type.icon;
        if (sprite != null) {
            resourceIcon.sprite = sprite;
        }
        resourceAmountText.text = amount.ToString();
    }
}
