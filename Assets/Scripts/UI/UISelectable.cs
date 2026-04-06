using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Add this component to any UI element (Button, Panel, upgrade node, etc.)
/// to make it participate in the selection indicator system.
///
/// The component can either:
///   (a) Inherit the global SelectionMode from SelectionManager, or
///   (b) Override it per-element for mixed menus (e.g. hover tabs, click upgrades).
/// </summary>
[DisallowMultipleComponent]
public class UISelectable : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerClickHandler,
    ISelectHandler,
    IDeselectHandler,
    ISubmitHandler {

    [Header("Mode Override")]
    [Tooltip("When false this element uses SelectionManager.DefaultMode.")]
    [SerializeField] private bool _overrideMode = false;
    [ShowIf("_overrideMode")]
    [SerializeField] private SelectionMode _modeOverride = SelectionMode.Hover;

    [Header("Target Rect")]
    [Tooltip("The RectTransform the indicator should size itself to. " +
             "Defaults to this GameObject's RectTransform if left empty. " +
             "Useful when the clickable area is smaller than the visual area.")]
    [SerializeField] private RectTransform _indicatorTarget;


    private RectTransform _rect;
    private bool _isPointerOver;


    private void Awake() {
        _rect = GetComponent<RectTransform>();
        if (_indicatorTarget == null) _indicatorTarget = _rect;
    }

    private SelectionMode ActiveMode =>
        _overrideMode ? _modeOverride : UISelectionManager.Instance.DefaultMode;

    /// <summary>Mouse enters the element.</summary>
    public void OnPointerEnter(PointerEventData eventData) {
        _isPointerOver = true;

        if (ActiveMode == SelectionMode.Hover)
            UISelectionManager.Instance.SetHighlight(_indicatorTarget);
    }

    /// <summary>Mouse leaves the element.</summary>
    public void OnPointerExit(PointerEventData eventData) {
        _isPointerOver = false;
        //Debug.Log("OnPinterExit");
        // Only clear if this element currently owns the highlight.
        if (UISelectionManager.Instance.CurrentHighlight == _indicatorTarget)
            UISelectionManager.Instance.ClearHighlight(force: false);
    }

    /// <summary>Mouse click on the element.</summary>
    public void OnPointerClick(PointerEventData eventData) {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        if (ActiveMode == SelectionMode.Click)
            UISelectionManager.Instance.SetSelected(_indicatorTarget);
        // In Hover mode the indicator is already here from OnPointerEnter;
        // we still fire SetSelected so listeners can react to the commit.
        else
            UISelectionManager.Instance.SetSelected(_indicatorTarget);
    }

    /// <summary>
    /// Unity's EventSystem calls this when a controller or keyboard navigates
    /// to this element. Treat it the same as a hover.
    /// </summary>
    public void OnSelect(BaseEventData eventData) {
        // Controller navigation always moves the highlight regardless of mode —
        // if you're navigating to it, you want to see the indicator on it.
        UISelectionManager.Instance.SetHighlight(_indicatorTarget);
    }

    public void OnDeselect(BaseEventData eventData) {
        if (UISelectionManager.Instance.CurrentHighlight == _indicatorTarget)
            UISelectionManager.Instance.ClearHighlight(force: false);
    }

    public void OnSubmit(BaseEventData eventData) {
        UISelectionManager.Instance.SetSelected(_indicatorTarget);
    }
}