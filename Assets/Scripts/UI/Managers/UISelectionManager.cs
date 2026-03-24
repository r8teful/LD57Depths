using System;
using UnityEngine;

/// <summary>
/// Governs how hovering and clicking interact with the selection indicator.
/// </summary>
public enum SelectionMode {
    /// <summary>
    /// Moving the cursor over an element immediately highlights it.
    /// Ideal for mouse-driven menus.
    /// </summary>
    Hover,

    /// <summary>
    /// An explicit click or controller-confirm is required to move the indicator.
    /// Ideal for controller-driven menus, or upgrade trees where browsing ≠ selecting.
    /// </summary>
    Click
}
public class UISelectionManager : Singleton<UISelectionManager> {

    [SerializeField] private SelectionMode _defaultMode = SelectionMode.Hover;

    [Tooltip("When true, the indicator persists on the last selected element after the " +
             "cursor leaves, until a new element is highlighted or ClearAll is called.")]
    [SerializeField] private bool _persistOnMouseExit = true;

    /// <summary>Fired when a new rect is highlighted (hover or controller nav).</summary>
    public static event Action<RectTransform> OnHighlighted;

    /// <summary>Fired when a rect is explicitly selected (click or controller confirm).</summary>
    public static event Action<RectTransform> OnSelected;

    /// <summary>Fired when the highlight is fully cleared.</summary>
    public static event Action<bool> OnCleared;

    public RectTransform CurrentHighlight { get; private set; }
    public RectTransform CurrentSelection { get; private set; }
    public SelectionMode DefaultMode => _defaultMode;
    public bool PersistOnMouseExit => _persistOnMouseExit;

    /// <summary>
    /// Move the indicator to <paramref name="target"/> without committing a selection.
    /// Called automatically by SelectableUI on pointer-enter / controller navigation.
    /// </summary>
    public void SetHighlight(RectTransform target) {
        if (target == null) return;
        CurrentHighlight = target;
        OnHighlighted?.Invoke(target);
    }

    /// <summary>
    /// Commit <paramref name="target"/> as the active selection (click / confirm).
    /// Also updates the highlight to match.
    /// </summary>
    public void SetSelected(RectTransform target) {
        if (target == null) return;
        CurrentSelection = target;
        SetHighlight(target);          // selection always moves the indicator
        OnSelected?.Invoke(target);
    }

    /// <summary>
    /// Clear the hover highlight. If <paramref name="force"/> is false the manager
    /// respects the PersistOnMouseExit setting (the indicator stays on the last target).
    /// Pass force=true to always clear (e.g. on menu close).
    /// </summary>
    public void ClearHighlight(bool force = false) {
        if (!force && _persistOnMouseExit) return;

        CurrentHighlight = null;
        if (!force) CurrentSelection = null;
        OnCleared?.Invoke(true);
    }

    public void ClearAll() {
        CurrentHighlight = null;
        CurrentSelection = null;
        OnCleared?.Invoke(true);
    }

    /// <summary>Switch the global mode at runtime (e.g. when a controller is connected).</summary>
    public void SetMode(SelectionMode mode) => _defaultMode = mode;
}