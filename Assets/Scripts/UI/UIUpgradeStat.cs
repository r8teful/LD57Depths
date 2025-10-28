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

    internal void Init(StatType stat, float valueNow, float valueLater = -1) {
        _statName.text = GetStatString(stat);
        _statNow.text = valueNow.ToString();
        _statLater.color = IsLowerBad(stat) && valueLater > valueNow ? Color.green : Color.red;
        if(valueLater < 0) {
            // Dissable
            _statLater.gameObject.SetActive(false);
            _arrowImage.SetActive(false);
        } else {
            _statLater.text = valueLater.ToString();
            _arrowImage.SetActive(true);
        }
    }
    internal void Init(StatChangeStatus status) {
        var stat = status.StatType;
        var valueNow = status.ValueNow;
        var valueLater = status.ValueNext;
        Init(stat, valueNow, valueLater);
    }

    private string GetStatString(StatType stat) {
        switch (stat) {
            case StatType.MiningRange:
                return "Range";
            case StatType.MiningDamage:
                return "Damage";
            case StatType.MiningRotationSpeed:
                return "Movement Speed";
            case StatType.PlayerSpeedMax:
                return "Max Speed";
            case StatType.PlayerAcceleration:
                return "Acceleration";
            case StatType.PlayerOxygenMax:
                return "Max Oxygen";
            case StatType.MiningKnockback:
                return "Knockback Force";
                
            case StatType.MiningFalloff:
                return "Damage Falloff";
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
            case StatType.MiningRotationSpeed:
                return true;
            case StatType.PlayerSpeedMax:
                return true;
            case StatType.PlayerAcceleration:
                return true;
            case StatType.PlayerOxygenMax:
                return true;
            case StatType.MiningKnockback:
                return false;
            case StatType.MiningFalloff:
                return false;
            default:
                return true;
        }
    }
}