using UnityEngine;
using UnityEngine.UI.Extensions;

public class UIUpgradeLine : MonoBehaviour {
    private static readonly string LINE_PURCHASED_HEX = "#D58141";
    private static readonly string LINE_AVAILABLE_HEX = "#9D1952";
    private static readonly string LINE_NOT_AVAILABLE_HEX = "#20062D";
    private Color _linePurchasedColor;
    private Color _lineAvailableColor;
    private Color _lineNotAvailableColor;
    private UpgradeNodeState _upgradeNodeStateFrom;
    private bool _isToPurchasedOnce;
    private UpgradeNodeState _upgradeNodeStateTo;
    private UILineRenderer _line;
    private Material _mat;
    
    private void Awake() {
        ColorUtility.TryParseHtmlString(LINE_PURCHASED_HEX, out _linePurchasedColor);
        ColorUtility.TryParseHtmlString(LINE_AVAILABLE_HEX, out _lineAvailableColor);
        ColorUtility.TryParseHtmlString(LINE_NOT_AVAILABLE_HEX, out _lineNotAvailableColor);
    }
    public void Init(UIUpgradeNode from, UIUpgradeNode to,UILineRenderer myLine) {
        _mat = myLine.material;
        from.OnStateChange += StateChangeFrom;
        to.OnStateChange += StateChangeTo;
        _line = myLine;
        _line.material = new(_mat); // apply
        _line.material.SetFloat("_Tilt",GetTiltValue(from.transform.position, to.transform.position));
        // Init with initial node states
        _upgradeNodeStateFrom = from.GetState;
        _upgradeNodeStateTo = to.GetState;
        UpdateColor();
    }
    private void StateChangeTo(UpgradeNodeState state, bool isPurchasedOnce) {
        _upgradeNodeStateTo = state;
        _isToPurchasedOnce = isPurchasedOnce;
        UpdateColor();
    }

    private void StateChangeFrom(UpgradeNodeState state, bool isPurchasedOnce) {
        _upgradeNodeStateFrom = state;
        UpdateColor();
    }
    private void UpdateColor() {
        if (_upgradeNodeStateTo == UpgradeNodeState.Locked) {
            var c = Color.red;
            c.a = 0;
            _line.color = c;
            // No animation
            _line.material.SetFloat("_Intensity", 0);
        } else if (_upgradeNodeStateFrom == UpgradeNodeState.Purchased && _upgradeNodeStateTo == UpgradeNodeState.Purchased) {
            // Both purchased
            _line.color = _linePurchasedColor;
            // Animation pasive 
            _line.material.SetFloat("_Intensity", 0.3f);
        } else if (_upgradeNodeStateFrom != UpgradeNodeState.Locked) {
            if (_upgradeNodeStateTo == UpgradeNodeState.Purchasable || _isToPurchasedOnce) {
                // next is available, or we've already purchsed it once make line blue
                _line.color = _lineAvailableColor;
                _line.material.SetFloat("_Intensity", 3);
            } else if (_upgradeNodeStateTo == UpgradeNodeState.Unlocked) {
                _line.color = _lineNotAvailableColor;
            }
        }
    }
    private float GetTiltValue(Vector3 from, Vector3 to) {
        if(Mathf.Abs(from.x - to.x) < 0.01f) {
            // completely vertical 
            return -1;
        }
        // completely horizontal or otherwise
        return 0;
    }
}