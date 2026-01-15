using System;
using TMPro;
using UnityEngine;

// A player stat can also be a tool stat, etc... Used in upgrade screen to show the numerical values of a statistic
public class UIUpgradeStat : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI _statNow;
    [SerializeField] private TextMeshProUGUI _statName;
    [SerializeField] private TextMeshProUGUI _statLater;
    [SerializeField] private GameObject _arrowImage;

    private void OnEnable() {
       // UIUpgradeScreen.OnSelectedNodeChanged += OnUpgradeChanged;
    }
    private void OnDisable() {
       // UIUpgradeScreen.OnSelectedNodeChanged -= OnUpgradeChanged;
    }
    private void OnUpgradeChanged(UpgradeRecipeSO sO) {
        // Change to "unselected" visual if matching
    }

    internal void Init(StatChangeStatus status) {
        var stat = status.StatName;
        var valueNow = status.ValueNow;
        var valueLater = status.ValueNext;
        var isBad = status.IsBadChange;
        _statName.text = stat;
        _statNow.text = valueNow;
        _statLater.color = isBad ? Color.red : Color.green;
        if (valueLater == String.Empty) {
            // Dissable
            _statLater.gameObject.SetActive(false);
            _arrowImage.SetActive(false);
        } else {
            _statLater.text = valueLater;
            _arrowImage.SetActive(true);
        }
    }
}