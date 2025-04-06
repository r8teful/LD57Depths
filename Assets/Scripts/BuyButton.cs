using UnityEngine;
using System.Collections;

public class BuyButton : MonoBehaviour {
    public UpgradeType type;
    public void OnClick() {
        UpgradeManager.Instance.BuyUpgrade(type);
    }
}