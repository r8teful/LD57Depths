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
        UIUpgradeScreen.OnSelectedUpgradeChanged += OnUpgradeChanged;
    }
    private void OnDisable() {
        UIUpgradeScreen.OnSelectedUpgradeChanged -= OnUpgradeChanged;
    }
    private void OnUpgradeChanged(UpgradeRecipeSO sO) {
        // Change to "unselected" visual if matching
    }

    internal void Init(StatType stat, float valueNow, float valueLater = -1) {
        _statName.text = GetStatString(stat);
        _statNow.text = valueNow.ToString();
        if(valueLater < 0) {
            // Dissable
            _statLater.gameObject.SetActive(false);
            _arrowImage.SetActive(false);
        } else {
            _statLater.text = valueLater.ToString();
            _arrowImage.SetActive(true);
        }
    }
    
    private string GetStatString(StatType stat) {
        switch (stat) {
            case StatType.MiningRange:
                return "Range";
            case StatType.MiningDamage:
                return "Damage";
            case StatType.MiningHandling:
                return "Handling";
            case StatType.PlayerSpeedMax:
                return "Max Speed";
            case StatType.PlayerAcceleration:
                return "Acceleration";
            case StatType.PlayerOxygenMax:
                return "Max Oxygen";
            case StatType.PlayerLightRange:
                return "Light Range";
            case StatType.PlayerLightIntensity:
                return "Light Intensity";
            default:
                return "NULL";
        }
    }
    private bool IsLowerBad(StatType stat) {
        switch (stat) {
            case StatType.MiningRange:
                return true;
            case StatType.MiningDamage:
                return true;
            case StatType.MiningHandling:
                return true;
            case StatType.PlayerSpeedMax:
                return true;
            case StatType.PlayerAcceleration:
                return true;
            case StatType.PlayerOxygenMax:
                return true;
            case StatType.PlayerLightRange:
                return true;
            case StatType.PlayerLightIntensity:
                return true;
            default:
                return true;
        }
    }
}