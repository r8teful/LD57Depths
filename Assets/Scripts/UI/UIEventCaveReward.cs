using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIEventCaveReward : MonoBehaviour, IUIReward {
    [SerializeField] private Image itemImage; 
    [SerializeField] private TextMeshProUGUI itemQuantity; 
    [SerializeField] private TextMeshProUGUI text; 

    public void Init(UIRewardScreenBase parent, IExecutable reward) {

        var status = reward.GetExecuteStatus();
        if (status == null) return;
        if(status is ItemGainStatus item) {
            itemQuantity.text = item.ItemQuantity.quantity.ToString();
            itemImage.sprite = item.ItemQuantity.item.icon;
        }
    }
}