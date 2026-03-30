using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles panning and zooming of a child RectTransform within a viewport (parent) RectTransform.
/// Supports both pointer/touch input and controller input (zoom only, focus-based navigation).
/// </summary>
public class PanAndZoomController : MonoBehaviour {
    [Header("References")]
    private RectTransform viewportRect;   // The parent / clipping bounds
    private RectTransform contentRect;    // The child being panned / zoomed

    [Header("Zoom Settings")]
    [SerializeField] private float minZoom = 0.5f;
    [SerializeField] private float maxZoom = 3.0f;
    [SerializeField] private float zoomSmoothSpeed = 8f;
    [SerializeField] private float zoomStep = 0.1f;        // Multiplied by raw zoom input

    [Header("Pan Settings")]
    [SerializeField] private float panSmoothSpeed = 12f;

    [SerializeField] private float boundsPadding = 150f;



    private InputManager _inputManager;
    private UIUpgradeTree _upgradeTree;

    private bool _isController = false;
    private bool _isDragging;
    private bool _isZooming;

    public bool IsDraggingOrZooming => _isDragging || _isZooming;

    private Vector2 lastPointerPosition;

    private float zoomAmount;
    private float _targetScale = 1f;
    private Vector2 _targetPosition;

    private Coroutine focusCoroutine;
    private bool _initialized;


    public void Init(InputManager input, UIUpgradeTree tree) {
        viewportRect = GetComponent<RectTransform>();
        contentRect = transform.GetChild(0).GetComponent<RectTransform>();
        _inputManager = input;
        _upgradeTree = tree;
        _targetScale = contentRect.localScale.x;
        _targetPosition = contentRect.anchoredPosition;
        _initialized = true;
        RecalculateContentBounds();
        InputManager.OnDeviceChanged += DeviceChange;
    }
    private void OnDestroy() {
        InputManager.OnDeviceChanged -= DeviceChange;
    }

    private void DeviceChange(InputManager.DeviceType device) {
        if (device == InputManager.DeviceType.Gamepad) {
            _isController = true;
            _isDragging = false; // stop dragging
        } else if (device == InputManager.DeviceType.KeyboardMouse) {
            _isController = false;
        }
    }

    private void Update() {
        if (!_initialized) return;
        HandleZoom();

        if (!_isController) {
            HandleDragMouseKeyboard();
        } else {
            HandleDragController();
        }
        ApplySmoothTransform();
        //ApplyTransform();
    }



    /// <summary>Called by InputManager when a drag/pan gesture begins.</summary>
    internal void OnPanStart() {
        if (_isController) return;

        var input = _inputManager.GetAimScreenInput();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            viewportRect, input, null, out lastPointerPosition);
        _isDragging = true;
    }

    /// <summary>Called by InputManager when a drag/pan gesture ends.</summary>
    internal void OnPanEnd() {
        _isDragging = false;
    }

    /// <summary>Called by InputManager each frame with the current zoom delta.</summary>
    public void OnZoom(float zoom) {
        zoomAmount = zoom;
    }
    public void OnZoomStart(bool isZoomIn) {
        zoomAmount = isZoomIn ? 1 : -1 ; // 
    }
    public void OnZoomEnd() {
        zoomAmount = 0;
    }

    private void HandleDragMouseKeyboard() {
        if (!_isDragging) return;

        var input = _inputManager.GetAimScreenInput();

        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                viewportRect, input, null, out Vector2 currentPointerPosition))
            return;

        // Delta is in the viewport's local space; add it directly to the
        // content's anchored position so dragging feels 1-to-1.
        Vector2 delta = currentPointerPosition - lastPointerPosition;
        lastPointerPosition = currentPointerPosition;

        _targetPosition += delta;

        // Immediately clamp so the smoothed position never chases an
        // out-of-bounds target.
        _targetPosition = ClampedPosition(_targetPosition, _targetScale);
    }

    private void HandleDragController() {
        Vector2 panInput = _inputManager.GetPanVector() * 3;
        _targetPosition += panInput;
        _targetPosition = ClampedPosition(_targetPosition, _targetScale);
    }

    private void HandleZoom() {
        if (zoomAmount == 0f) return;

        float newScale = Mathf.Clamp(
            _targetScale + zoomAmount * zoomStep,
            minZoom,
            maxZoom);

        // Zoom toward the current anchor-center so content doesn't jump.
        // (For pointer-based zooming you can replace Vector2.zero with the
        //  pointer's local position inside the viewport.)
        var toward = _inputManager.GetPointerPivotInViewport(viewportRect);
        ZoomToward(newScale, toward);

        zoomAmount = 0f;
    }

    /// <summary>
    /// Adjusts scale and repositions content so that <paramref name="pivot"/>
    /// (in viewport local space) stays stationary under the zoom.
    /// </summary>
    private void ZoomToward(float newScale, Vector2 pivot) {
        // How much did the scale change?
        float scaleDelta = newScale / _targetScale;

        // Offset the content position so the pivot point is fixed.
        _targetPosition = pivot + (_targetPosition - pivot) * scaleDelta;
        _targetScale = newScale;

        // Clamp after scaling – the new scale may expose out-of-bound areas.
        _targetPosition = ClampedPosition(_targetPosition, _targetScale);
    }



    private void ApplySmoothTransform() {
        float dt = Time.unscaledDeltaTime;

        float smoothedScale = Mathf.Lerp(
            contentRect.localScale.x,
            _targetScale,
            dt * zoomSmoothSpeed);

        Vector2 smoothedPos = Vector2.Lerp(
            contentRect.anchoredPosition,
            _targetPosition,
            dt * panSmoothSpeed);

        contentRect.localScale = Vector3.one * smoothedScale;
        contentRect.anchoredPosition = smoothedPos;
    }

    private void ApplyTransform() {
        contentRect.localScale = Vector3.one * _targetScale;
        contentRect.anchoredPosition = _targetPosition;

    }

    /// <summary>
    /// Ensures <paramref name="child"/> RectTransform is fully inside
    /// <paramref name="parent"/> RectTransform.
    /// Call after any positional or scale change.
    /// </summary>
    public static void ClampToParentBounds(RectTransform child, RectTransform parent) {
        // Work in the parent's local space.
        Vector2 childPos = child.anchoredPosition;
        Vector2 childSize = child.rect.size * child.localScale;   // scaled size
        Vector2 parentSize = parent.rect.size;

        // Child pivot offsets (how far the anchor is from each edge).
        Vector2 pivotOffset = child.pivot * childSize;

        // Min / max the child's anchor point can travel.
        float minX = pivotOffset.x - (childSize.x - parentSize.x) * 0.5f
                     - parentSize.x * parent.pivot.x;
        float maxX = parentSize.x - pivotOffset.x - (childSize.x - parentSize.x) * 0.5f
                     - parentSize.x * parent.pivot.x;

        float minY = pivotOffset.y - (childSize.y - parentSize.y) * 0.5f
                     - parentSize.y * parent.pivot.y;
        float maxY = parentSize.y - pivotOffset.y - (childSize.y - parentSize.y) * 0.5f
                     - parentSize.y * parent.pivot.y;

        // When the content is smaller than the viewport, center it instead
        // of clamping (prevents it from "escaping" the opposite side).
        if (childSize.x <= parentSize.x) {
            childPos.x = 0f;
        } else {
            childPos.x = Mathf.Clamp(childPos.x, minX, maxX);
        }

        if (childSize.y <= parentSize.y) {
            childPos.y = 0f;
        } else {
            childPos.y = Mathf.Clamp(childPos.y, minY, maxY);
        }

        child.anchoredPosition = childPos;
    }

    /// <summary>
    /// Returns the clamped version of <paramref name="position"/> for the
    /// current viewport/content pair at a given <paramref name="scale"/>,
    /// without actually applying it (lets us clamp the *target* position).
    /// </summary>
    private Vector2 ClampedPosition(Vector2 position, float scale) {
        Vector2 contentSize = contentRect.rect.size * scale;
        Vector2 parentSize = viewportRect.rect.size;

        Vector2 pivotOffset = contentRect.pivot * contentSize;

        float minX = pivotOffset.x;
        float maxX = contentSize.x - pivotOffset.x;
        float minY = pivotOffset.y;
        float maxY = contentSize.y - pivotOffset.y;

        // Horizontal
        if (contentSize.x <= parentSize.x) {
            position.x = 0f;
        } else {
            float halfExtra = (contentSize.x - parentSize.x) * 0.5f;
            position.x = Mathf.Clamp(position.x, -halfExtra, halfExtra);
        }

        // Vertical
        if (contentSize.y <= parentSize.y) {
            position.y = 0f;
        } else {
            float halfExtra = (contentSize.y - parentSize.y) * 0.5f;
            position.y = Mathf.Clamp(position.y, -halfExtra, halfExtra);
        }

        return position;
    }


    /// <summary>
    /// Smoothly pans the content so <paramref name="targetNode"/> is centered
    /// inside the viewport.  Controller-only: panning is never allowed directly.
    /// </summary>
    public void FocusOnNode(RectTransform targetNode) {
        if (targetNode == null) return;

        if (focusCoroutine != null) {
            StopCoroutine(focusCoroutine);
        }

        focusCoroutine = StartCoroutine(FocusOnNodeInternal(targetNode));
    }

    private IEnumerator FocusOnNodeInternal(RectTransform targetNode) {
        Vector3 target = GetFocusNodeTarget(targetNode);

        yield return FocusOnNodeRoutine(target);

        focusCoroutine = null;
    }

    /// <summary>
    /// Calculates the anchored position the content must move to in order to
    /// center <paramref name="targetNode"/> inside the viewport.
    /// </summary>
    private Vector3 GetFocusNodeTarget(RectTransform targetNode) {
        // Convert the node's world center to the content's local space, then
        // negate it so moving the content by that delta centers the node.
        Vector2 nodeWorldCenter = targetNode.TransformPoint(targetNode.rect.center);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            contentRect,
            RectTransformUtility.WorldToScreenPoint(null, nodeWorldCenter),
            null,
            out Vector2 localInContent);

        // The target anchored position centers the node in the viewport.
        Vector2 targetPos = -localInContent * contentRect.localScale.x;

        // Clamp so we never focus outside the valid pan area.
        targetPos = ClampedPosition(targetPos, _targetScale);

        return targetPos;
    }

    /// <summary>
    /// Smoothly moves the content to the pre-calculated <paramref name="target"/>
    /// position over time, updating <see cref="_targetPosition"/> so the normal
    /// smooth loop takes over once close enough.
    /// </summary>
    private IEnumerator FocusOnNodeRoutine(Vector3 target) {
        Vector2 destination = target;
        float elapsed = 0f;
        const float duration = 0.35f;
        Vector2 startPos = _targetPosition;

        while (elapsed < duration) {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            _targetPosition = Vector2.Lerp(startPos, destination, t);
            yield return null;
        }

        _targetPosition = destination;
    }




    /// <summary>Snaps content immediately to the clamped bounds (no smoothing).</summary>
    public void SnapToBounds() {
        ClampToParentBounds(contentRect, viewportRect);
        _targetPosition = contentRect.anchoredPosition;
    }
    public void RecalculateContentBounds() {
        if (_upgradeTree == null) return;

        IEnumerable<UIUpgradeNode> visibleNodes = _upgradeTree.GetVisibleNodes();

        // 1. Find the bounding box of all visible nodes in content local space 

        bool hasAny = false;
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;

        // Reusable buffer GetWorldCorners order: BL, TL, TR, BR.
        Vector3[] worldCorners = new Vector3[4];

        foreach (UIUpgradeNode node in visibleNodes) {
            RectTransform nodeRect = node.GetComponent<RectTransform>();
            if (nodeRect == null) continue;

            nodeRect.GetWorldCorners(worldCorners);

            foreach (Vector3 worldCorner in worldCorners) {
                // World-space corner  content rect local space.
                Vector3 local = contentRect.InverseTransformPoint(worldCorner);
                if (local.x < minX) minX = local.x;
                if (local.x > maxX) maxX = local.x;
                if (local.y < minY) minY = local.y;
                if (local.y > maxY) maxY = local.y;
            }

            hasAny = true;
        }

        if (!hasAny) {
            // No visible nodes  fall back to viewport size so clamping
            // still has a sensible reference area.
            contentRect.sizeDelta = viewportRect.rect.size;
            _targetPosition = ClampedPosition(_targetPosition, _targetScale);
            return;
        }


        minX -= boundsPadding;
        minY -= boundsPadding;
        maxX += boundsPadding;
        maxY += boundsPadding;

        Vector2 newSize = new Vector2(maxX - minX, maxY - minY);

        //  3. Resize the content rect
        // sizeDelta sets rect.size for a fixed-size rect (identical anchors).
        // Child node positions (anchoredPosition) are relative to the rect's
        // local origin, so they remain unchanged when we resize.

        contentRect.sizeDelta = newSize;

        //  4. Re-clamp pan target with the new rect size

        _targetPosition = ClampedPosition(_targetPosition, _targetScale);
    }
}