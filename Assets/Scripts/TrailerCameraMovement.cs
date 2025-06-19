using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

public class TrailerCameraMovement : MonoBehaviour {
    [Header("Movement Settings")]
    [SerializeField]
    private float targetSpeed = 5f;

    [SerializeField]
    private float accelerationTime = 1f;

    [SerializeField]
    private float decelerationTime = 1f;

    [Header("Direction")]
    [SerializeField]
    private Vector2 moveDirection = Vector2.up;

    [Header("Runtime Readout")]
    [ReadOnly]
    [SerializeField]
    private float currentSpeed = 0f;

    private Tweener speedTweener;
    private bool isMoving = false;

    private void Update() {
        // Move the camera along the custom direction
        if (currentSpeed > 0f) {
            Vector2 dir = moveDirection.normalized;
            Vector3 delta = new Vector3(dir.x, dir.y, 0f) * currentSpeed * Time.deltaTime;
            transform.position += delta;
        }
    }

    /// <summary>
    /// Starts smooth acceleration from current speed to target speed.
    /// </summary>
    [Button("Start Moving", ButtonSizes.Large)]
    private void StartMovement() {
        speedTweener?.Kill();
        speedTweener = DOTween.To(
            () => currentSpeed,
            x => currentSpeed = x,
            targetSpeed,
            accelerationTime
        ).SetEase(Ease.OutSine);

        isMoving = true;
        Debug.Log("TrailerCamera: Starting movement.");
    }

    /// <summary>
    /// Smoothly decelerates from current speed to zero.
    /// </summary>
    [Button("Stop Moving", ButtonSizes.Large)]
    private void StopMovement() {
        if (!isMoving)
            return;

        speedTweener?.Kill();
        speedTweener = DOTween.To(
            () => currentSpeed,
            x => currentSpeed = x,
            0f,
            decelerationTime
        ).SetEase(Ease.InSine).OnComplete(() => {
            isMoving = false;
            Debug.Log("TrailerCamera: Movement stopped.");
        });

        Debug.Log("TrailerCamera: Stopping movement.");
    }

    private void OnDisable() {
        speedTweener?.Kill();
    }

    // Draw the movement direction gizmo
    private void OnDrawGizmos() {
        Gizmos.color = Color.cyan;
        Vector3 start = transform.position;
        Vector3 end = start + (Vector3)moveDirection.normalized;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawSphere(end, 0.1f);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected() {
        Handles.color = Color.yellow;
        Vector3 handlePos = transform.position + (Vector3)moveDirection;
        Vector3 newHandlePos = Handles.FreeMoveHandle(
            handlePos,
            0.1f,
            Vector3.zero,
            Handles.SphereHandleCap
        );
        if (newHandlePos != handlePos) {
            Undo.RecordObject(this, "Change Move Direction");
            Vector2 newDir = new Vector2(
                newHandlePos.x - transform.position.x,
                newHandlePos.y - transform.position.y
            );
            moveDirection = newDir;
            EditorUtility.SetDirty(this);
        }
    }
#endif
}