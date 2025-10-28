using DG.Tweening;
using System.Collections;
using UnityEngine;

public class UpgradePanAndZoom : MonoBehaviour {
    private RectTransform treeContainer;
    [Header("Zoom Settings")]
    [SerializeField] private float zoomStep = 0.1f;
    [SerializeField] private float zoomSpeed = 0.1f;
    [SerializeField] private float minZoom = 0.5f;
    [SerializeField] private float maxZoom = 2f;

    [Header("Pan Settings")]
    [SerializeField] private float panSpeed = 20f;
    [SerializeField] private float damping = 10f;
    [SerializeField] private float tweenTime = 0.2f;
    private float zoomAmount;
    private Bounds treeLocalBounds;

    [Header("Controller Focus Settings")]
    [SerializeField] private float focusTime = 0.3f;
    private InputManager _inputManager;
    private RectTransform viewportRect;

    private Coroutine focusCoroutine;

    // For smooth damping of elastic bounds
    private Vector2 currentVelocity;
    private Vector2 lastPointerPosition;
    private bool _isDragging;


    public void Init(InputManager input) {
        _inputManager = input;
        viewportRect = GetComponent<RectTransform>();
        CalculateTreeBounds();
        // --- Key Change: Automatically find the tree container ---
        if (transform.childCount == 0) {
            Debug.LogError("UITreeNavigator requires a child object to act as the pannable tree container.", this);
            this.enabled = false;
            return;
        }

        treeContainer = transform.GetChild(0).GetComponent<RectTransform>();
        if (treeContainer == null) {
            Debug.LogError("The first child of the UITreeNavigator must have a RectTransform component.", this);
            this.enabled = false;
            return;
        }
    }
    void Update() {
        HandleZoom();
        if (_isDragging) {
            HandlePan();
        } else {
            // Apply elastic bounds when not dragging
            //ApplyElasticBounds();
            //EnforceBounds();
            EnsureSomeOverlapWithParent(treeContainer, viewportRect);
        }
    }

    public void OnZoom(float zoom) {
        zoomAmount = zoom;
    }
    internal void OnPanStart() {
        var input = _inputManager.GetAimScreenInput();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewportRect, input, null, out lastPointerPosition);
        _isDragging = true;
        currentVelocity = Vector2.zero; // Stop damping when user takes control
    }

    internal void OnPanStop() {
        _isDragging = false;
    }

    public void CalculateTreeBounds() {
        if (treeContainer == null || treeContainer.childCount == 0) {
            treeLocalBounds = new Bounds(Vector3.zero, Vector3.zero);
            return;
        }

        // Initialize bounds with the first child to have a starting point
        var firstChild = treeContainer.GetChild(0).GetComponent<RectTransform>();
        treeLocalBounds = new Bounds(firstChild.localPosition, Vector3.zero);

        // Encapsulate all children's rects
        foreach (RectTransform child in treeContainer) {
            // Get the corners of the child's rect in the container's local space
            Vector3 min = child.localPosition + new Vector3(child.rect.xMin, child.rect.yMin, 0);
            Vector3 max = child.localPosition + new Vector3(child.rect.xMax, child.rect.yMax, 0);

            treeLocalBounds.Encapsulate(min);
            treeLocalBounds.Encapsulate(max);
           // treeLocalBounds.Encapsulate(new Bounds(child.localPosition, child.rect.size));
        }
        Debug.Log($"Bounds Calculated. Center: {treeLocalBounds.center}, Size: {treeLocalBounds.size}");
    }

    private void HandleZoom() {
        if (Mathf.Abs(zoomAmount) < 0.1f) return;
        //Debug.Log(zoomAmount);
        float scroll = zoomAmount * zoomSpeed;

        float currentScale = treeContainer.localScale.x;
        float newScale = Mathf.Clamp(currentScale + scroll * currentScale, minZoom, maxZoom);

        if (Mathf.Approximately(currentScale, newScale)) return;

        Vector2 pointerScreenPos = _inputManager.GetAimScreenInput();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(viewportRect, pointerScreenPos, null, out Vector2 localPointerPosition);

        Vector2 pivotToMouse = localPointerPosition - (Vector2)treeContainer.localPosition;
        Vector2 newPivotPosition = pivotToMouse * (newScale / currentScale);
        Vector2 positionOffset = newPivotPosition - pivotToMouse;

        treeContainer.localScale = Vector3.one * newScale;
        treeContainer.localPosition -= (Vector3)positionOffset;

        ApplyElasticBounds(true);
    }

    private void HandlePan() {
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(viewportRect, _inputManager.GetAimScreenInput(), null, out Vector2 localPointerPosition))
            return;
        Vector2 delta = localPointerPosition - lastPointerPosition;
        treeContainer.anchoredPosition += delta * panSpeed;
        lastPointerPosition = localPointerPosition;
    }

    private Vector2 GetBoundsMin() {
        float scale = treeContainer.localScale.x;
        Vector2 viewportSize = viewportRect.rect.size;
        Vector2 scaledBoundsSize = treeLocalBounds.size * scale;
        Vector2 scaledBoundsCenter = treeLocalBounds.center * scale;

        return (viewportSize / 2f) - (scaledBoundsSize / 2f) - scaledBoundsCenter;
    }

    private Vector2 GetBoundsMax() {
        float scale = treeContainer.localScale.x;
        Vector2 viewportSize = viewportRect.rect.size;
        Vector2 scaledBoundsSize = treeLocalBounds.size * scale;
        Vector2 scaledBoundsCenter = treeLocalBounds.center * scale;

        return (-viewportSize / 2f) + (scaledBoundsSize / 2f) - scaledBoundsCenter;
    }

    private void ApplyElasticBounds(bool immediate = false) {
        if (treeLocalBounds.size == Vector3.zero) return;

        Vector2 min = GetBoundsMin();
        Vector2 max = GetBoundsMax();

        // If content is smaller than viewport, clamp position to center
        Vector2 scaledContentSize = treeLocalBounds.size * treeContainer.localScale.x;
        if (scaledContentSize.x < viewportRect.rect.width) {
            min.x = max.x = -treeLocalBounds.center.x * treeContainer.localScale.x;
        }
        if (scaledContentSize.y < viewportRect.rect.height) {
            min.y = max.y = -treeLocalBounds.center.y * treeContainer.localScale.y;
        }

        Vector2 currentPos = treeContainer.anchoredPosition;
        Vector2 targetPos = currentPos;
        targetPos.x = Mathf.Clamp(targetPos.x, min.x, max.x);
        targetPos.y = Mathf.Clamp(targetPos.y, min.y, max.y);

        if (immediate) {
            treeContainer.anchoredPosition = targetPos;
            currentVelocity = Vector2.zero;
            return;
        }

        // If not dragging and out of bounds, smoothly damp back to the target position
        if (!_isDragging && currentPos != targetPos) {
            treeContainer.anchoredPosition = Vector2.SmoothDamp(currentPos, targetPos, ref currentVelocity, damping, Mathf.Infinity, Time.deltaTime);
        }
    }

    /// <summary>
    /// Public method to move the view to focus on a specific UI element (node).
    /// </summary>
    /// <param name="targetNode">The RectTransform of the node to center in the view.</param>
    public void FocusOnNode(RectTransform targetNode) {
        if (targetNode == null) return;

        if (focusCoroutine != null) {
            StopCoroutine(focusCoroutine);
        }
        focusCoroutine = StartCoroutine(FocusOnNodeRoutine(targetNode));
    }

    private IEnumerator FocusOnNodeRoutine(RectTransform targetNode) {
        // Calculate the position required to center the target node.
        // The target's anchoredPosition is relative to the tree container's pivot.
        // We move the container in the opposite direction, scaled by the current zoom level.
        Vector3 targetPosition = -targetNode.anchoredPosition * treeContainer.localScale.x;

        // Clamp the target position to make sure we don't focus outside the valid bounds
        Vector2 min = GetBoundsMin();
        Vector2 max = GetBoundsMax();
        targetPosition.x = Mathf.Clamp(targetPosition.x, min.x, max.x);
        targetPosition.y = Mathf.Clamp(targetPosition.y, min.y, max.y);

        Vector3 startPosition = treeContainer.anchoredPosition;
        float timer = 0f;

        while (timer < focusTime) {
            timer += Time.deltaTime;
            float t = Mathf.SmoothStep(0.0f, 1.0f, timer / focusTime);
            treeContainer.anchoredPosition = Vector3.Lerp(startPosition, targetPosition, t);
            yield return null;
        }

        treeContainer.anchoredPosition = targetPosition;
        ApplyElasticBounds(true);
    }

  

    private void OnDrawGizmos() {
        if (treeContainer == null) return;

        // Gizmos work in world space, so we must convert our local bounds.
        Vector3 worldCenter = treeContainer.TransformPoint(treeLocalBounds.center);
        Vector3 worldSize = Vector3.Scale(treeContainer.lossyScale, treeLocalBounds.size);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(worldCenter, worldSize);
    }
    /// <summary>
    /// If the child RectTransform has no overlap with the parent RectTransform,
    /// nudge the child so that it at least touches/overlaps the parent.
    /// If they already overlap (even partially), do nothing.
    /// </summary>
    public void EnsureSomeOverlapWithParent(RectTransform child, RectTransform parent) {
        if (child == null || parent == null) return;

        // Bounds of child relative to parent's local space
        //Bounds childBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(parent, child);
        // Convert Bounds -> Rect (in parent's local space)
        //Rect childRect = new Rect(childBounds.min.x, childBounds.min.y, childBounds.size.x, childBounds.size.y);
        
        // ^ ^ ^ ^ ^ Do the above if you want the children to determine the bounds  
        
        Rect childRect = GetChildRectInParentSpace(child,parent);
        // Parent rect in parent local space
        Rect parentRect = parent.rect;

        // If they overlap already (even partially), do nothing.
        if (childRect.Overlaps(parentRect)) {
            return;
        }

        // Compute minimal offset (in parent's local space) to cause overlap/touch.
        Vector3 offset = Vector3.zero;

        // X axis: child is entirely left or right of parent
        if (childRect.xMax < parentRect.xMin) {
            // child is left of parent -> move right so child's right edge touches parent's left edge
            offset.x = parentRect.xMin - childRect.xMax;
        } else if (childRect.xMin > parentRect.xMax) {
            // child is right of parent -> move left so child's left edge touches parent's right edge
            offset.x = parentRect.xMax - childRect.xMin;
        }

        // Y axis: child is entirely below or above parent
        if (childRect.yMax < parentRect.yMin) {
            // child is below parent -> move up so child's top touches parent's bottom
            offset.y = parentRect.yMin - childRect.yMax;
        } else if (childRect.yMin > parentRect.yMax) {
            // child is above parent -> move down so child's bottom touches parent's top
            offset.y = parentRect.yMax - childRect.yMin;
        }

        if (offset != Vector3.zero) {
            // Convert offset from parent-local to the child's parent-local space and apply to child's localPosition
            Vector3 worldOffset = parent.TransformVector(offset); // parent local -> world
            Transform childParent = child.transform.parent;
            if (childParent != null) {
                Vector3 localOffsetForChild = childParent.InverseTransformVector(worldOffset);
                child.localPosition += localOffsetForChild;
                Vector3 targetLocalPos = child.localPosition + localOffsetForChild;
                child.DOKill();
                child.DOLocalMove(targetLocalPos, tweenTime);
            } else {
                child.localPosition += worldOffset;
            }
            
        }
    }
    /// <summary>
    /// Get a Rect representing the child's axis-aligned bounding box in the parent's local space,
    /// based on the child's world corners. This respects position, rotation and scale.
    /// </summary>
    public static Rect GetChildRectInParentSpace(RectTransform child, RectTransform parent) {
        if (child == null || parent == null) return new Rect();

        Vector3[] corners = new Vector3[4];
        child.GetWorldCorners(corners); // world-space corners in order: BL, TL, TR, BR

        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);

        for (int i = 0; i < 4; i++) {
            // Convert corner from world space into parent's local space
            Vector3 localPoint = parent.InverseTransformPoint(corners[i]);
            min.x = Mathf.Min(min.x, localPoint.x);
            min.y = Mathf.Min(min.y, localPoint.y);
            max.x = Mathf.Max(max.x, localPoint.x);
            max.y = Mathf.Max(max.y, localPoint.y);
        }

        return new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
    }
    /// <summary>
    /// Ensures 'child' RectTransform is fully inside 'parent' RectTransform.
    /// Call after you change child position/scale (e.g. on drag / on scale / in LateUpdate).
    /// Works when child is a direct or nested descendant of parent.
    /// </summary>
    /// <param name="child">The RectTransform being moved/scaled</param>
    /// <param name="parent">The RectTransform that bounds the child</param>
    public static void ClampToParentBounds(RectTransform child, RectTransform parent) {
        if (child == null || parent == null) return;

        // Calculate the child's bounds relative to the parent's local space
        var childBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(parent, child);

        // Parent rect in parent's local space
        Rect parentRect = parent.rect;

        // Compute how much the child bounds are out of the parent rect on each axis
        Vector3 offset = Vector3.zero;

        // X axis
        if (childBounds.min.x < parentRect.xMin)
            offset.x = parentRect.xMin - childBounds.min.x;
        else if (childBounds.max.x > parentRect.xMax)
            offset.x = parentRect.xMax - childBounds.max.x;
        // Y axis
        if (childBounds.min.y < parentRect.yMin)
            offset.y = parentRect.yMin - childBounds.min.y;
        else if (childBounds.max.y > parentRect.yMax)
            offset.y = parentRect.yMax - childBounds.max.y;

        // Apply the offset to the child's localPosition.
        // Because childBounds was computed relative to parent local-space, we can safely change localPosition
        // even when the child is nested, because localPosition is in the parent's local space only if parent is direct parent.
        // For nested children, we need to move the root transform that was used in CalculateRelativeRectTransformBounds:
        // Use Transform.InverseTransformPoint to convert the offset into the child's local space delta.
        // The simplest safe approach: translate the child's world position by the offset expressed as parent-local,
        // then convert back to child's local space and update anchoredPosition or localPosition.
        if (offset != Vector3.zero) {
            // Offset is in parent's local space; convert to world space then to child's parent local space
            Vector3 worldOffset = parent.TransformVector(offset); // parent local -> world
            Transform childParent = child.transform.parent;
            if (childParent != null) {
                Vector3 localOffsetForChild = childParent.InverseTransformVector(worldOffset);
                child.localPosition += localOffsetForChild;
            } else {
                // unlikely for UI, but fallback
                child.localPosition += worldOffset;
            }
        }
    }
}