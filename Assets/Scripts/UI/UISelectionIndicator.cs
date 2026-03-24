using DG.Tweening;
using System.Collections;
using UnityEngine;

/// <summary>
/// The visual indicator that frames the currently highlighted/selected UI element.
/// The indicator samples the target's world corners each frame so it correctly
/// handles elements inside scroll views, nested layouts, and any canvas scale.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class UISelectionIndicator : MonoBehaviour {

    [Tooltip("The root Canvas this indicator lives on. Auto-resolved if left empty.")]
    [SerializeField] private Canvas _rootCanvas;

    [Tooltip("Smoothly lerp position and size toward the target each frame.")]
    [SerializeField] private bool _animate = true;

    [Tooltip("Higher = snappier. 20 is a good game-feel starting point.")]
    [SerializeField, Range(1f, 50f)] private float _lerpSpeed = 20f;

    [SerializeField] private bool _snapOnFirstTarget = true;

    [Tooltip("Extra pixels added to each side of the target rect. " +
             "Lets the border sprite breathe without needing oversized images.")]
    [SerializeField] private Vector2 _padding = Vector2.zero;

    [Header("Visibility")]
    [Tooltip("Fade the indicator's CanvasGroup alpha when no target is active.")]
    [SerializeField] private bool _fadeWhenInactive = true;
    [SerializeField, Range(0f, 1f)] private float _inactiveAlpha = 0f;
    [SerializeField, Range(0f, 1f)] private float _activeAlpha = 1f;
    [SerializeField] private float _fadeDuration = 0.15f;


    private RectTransform _rect;
    private CanvasGroup _group;
    private RectTransform _target;
    private bool _hasTarget;
    private bool _firstTarget = true;

    // Working buffer - avoids allocations in Update.
    private readonly Vector3[] _corners = new Vector3[4];


    private void Awake() {
        _rect = GetComponent<RectTransform>();

        _rect.pivot = new Vector2(0.5f, 0.5f);
        _rect.anchorMin = new Vector2(0.5f, 0.5f);
        _rect.anchorMax = new Vector2(0.5f, 0.5f);

        // Ensure a CanvasGroup exists for fade support.
        _group = GetComponent<CanvasGroup>();
        if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();

        _group.blocksRaycasts = false;
        _group.interactable = false;

        // Resolve root canvas.
        if (_rootCanvas == null) _rootCanvas = GetComponentInParent<Canvas>().rootCanvas;

        // Start invisible if fading is on.
        if (_fadeWhenInactive) _group.alpha = _inactiveAlpha;
    }

    private void OnEnable() {
        UISelectionManager.OnHighlighted += OnHighlighted;
        UISelectionManager.OnCleared += OnCleared;
    }

    private void OnDisable() {
        UISelectionManager.OnHighlighted -= OnHighlighted;
        UISelectionManager.OnCleared -= OnCleared;
    }


    private void OnHighlighted(RectTransform target) {
        _target = target;
        _hasTarget = true;

        if (_fadeWhenInactive) {
            _group.DOFade(1, 0.2f);
        } else {
            _group.alpha = 1;
        }
    }

    private void OnCleared(bool forceNoFade) {
        _target = null;
        _hasTarget = false;
        if (_fadeWhenInactive && !forceNoFade) {
            _group.DOFade(0, 0.2f);
        } else {
            _group.alpha = 0;
        }
    }

    private void LateUpdate() {
        if (!_hasTarget || _target == null) return;
        // Sample world-space corners to get a position that is correct across
        // any canvas nesting, scroll view, or layout group.
        _target.GetWorldCorners(_corners);
        Vector3 worldCenter = (_corners[0] + _corners[2]) * 0.5f;

        Camera uiCam = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : _rootCanvas.worldCamera;

        RectTransform parent = _rect.parent as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parent,
            RectTransformUtility.WorldToScreenPoint(uiCam, worldCenter),
            uiCam,
            out Vector2 localCenter);

        // sizeDelta reflects the target's authored rect size — without any
        // runtime zoom baked in. Padding is added here in canvas units.
        Vector2 intrinsicSize = _target.rect.size + _padding * 2f;

        // Derive how much the target has been scaled relative to the root canvas.
        // This cleanly separates "how big is the rect" from "how zoomed is it",
        // so the border sprite scales uniformly instead of stretching.
        Vector3 canvasLossy = _rootCanvas.transform.lossyScale;
        Vector3 targetLossy = _target.lossyScale;

        Vector3 targetScale = new Vector3(
            canvasLossy.x > 0f ? targetLossy.x / canvasLossy.x : 1f,
            canvasLossy.y > 0f ? targetLossy.y / canvasLossy.y : 1f,
            1f);

        if (_firstTarget && _snapOnFirstTarget) {
            _rect.anchoredPosition = localCenter;
            _rect.sizeDelta = intrinsicSize;
            _rect.localScale = targetScale;
            _firstTarget = false;
        } else if (_animate) {
            float t = 1f - Mathf.Exp(-_lerpSpeed * Time.unscaledDeltaTime);
            _rect.anchoredPosition = Vector2.Lerp(_rect.anchoredPosition, localCenter, t);
            _rect.sizeDelta = Vector2.Lerp(_rect.sizeDelta, intrinsicSize, t);
            _rect.localScale = Vector3.Lerp(_rect.localScale, targetScale, t);
        } else {
            _rect.anchoredPosition = localCenter;
            _rect.sizeDelta = intrinsicSize;
            _rect.localScale = targetScale;
        }
    }
   
    /// <summary>
    /// Force an immediate snap to a target (e.g. when opening a menu and you
    /// want the indicator to start on the default button without sliding in).
    /// </summary>
    public void SnapTo(RectTransform target) {
        _firstTarget = true;
        OnHighlighted(target);
    }
}