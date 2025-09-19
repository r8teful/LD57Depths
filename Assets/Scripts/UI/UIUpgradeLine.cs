using System;
using UnityEngine;
using UnityEngine.UI.Extensions;
using static UIUpgradeNode;

public class UIUpgradeLine : MonoBehaviour {
    private static readonly string LINE_PURCHASED_HEX = "#D58141";
    private static readonly string LINE_AVAILABLE_HEX = "#3DB2AD";
    private static readonly string LINE_NOT_AVAILABLE_HEX = "#10325B";
    private Color _linePurchasedColor;
    private Color _lineAvailableColor;
    private Color _lineNotAvailableColor;
    private UpgradeNodeState _upgradeNodeStateFrom;
    private UpgradeNodeState _upgradeNodeStateTo;
    private UILineRenderer _line;

    private void Awake() {
        ColorUtility.TryParseHtmlString(LINE_PURCHASED_HEX, out _linePurchasedColor);
        ColorUtility.TryParseHtmlString(LINE_AVAILABLE_HEX, out _lineAvailableColor);
        ColorUtility.TryParseHtmlString(LINE_NOT_AVAILABLE_HEX, out _lineNotAvailableColor);
    }
    public void Init(UIUpgradeNode from, UIUpgradeNode to,UILineRenderer myLine) {
        from.OnStateChange += StateChangeFrom;
        to.OnStateChange += StateChangeTo;
        _line = myLine;
    }

    private void StateChangeTo(UpgradeNodeState state) {
        _upgradeNodeStateTo = state;
        if(gameObject.name == "Line LazerDamage1_Instance (UpgradeRecipeSO)") {
            Debug.Log("State To change to: " + state);
        }
        UpdateColor();
    }

    private void StateChangeFrom(UpgradeNodeState state) {
        if (gameObject.name == "Line LazerDamage1_Instance (UpgradeRecipeSO)") {
            Debug.Log("State From change to: " + state);
        }
        _upgradeNodeStateFrom = state;
        UpdateColor();
    }
    private void UpdateColor() {
        if(_upgradeNodeStateFrom == UpgradeNodeState.Purchased && _upgradeNodeStateTo == UpgradeNodeState.Purchased) {
            // Both purchased
            _line.color = _linePurchasedColor;
        } else if (_upgradeNodeStateFrom == UpgradeNodeState.Purchased && _upgradeNodeStateTo == UpgradeNodeState.Active) {
            // Just from purchased, next is available, make line blue
            _line.color = _lineAvailableColor;
        } else {
            _line.color = _lineNotAvailableColor;

        }
    }
}