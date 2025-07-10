using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIUpgradeResourceDisplay : MonoBehaviour {
    [SerializeField] private Image resourceIcon;
    [SerializeField] private TextMeshProUGUI amountText;
    public void Init(Sprite icon, int amount) {
        resourceIcon.sprite = icon;
        amountText.text = amount.ToString();
    }
}
