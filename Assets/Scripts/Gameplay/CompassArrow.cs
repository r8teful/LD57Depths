using UnityEngine;
using UnityEngine.UI;
public class CompassArrow : MonoBehaviour {
    private bool _isPlus;
    private Transform _player;
    private Transform _targetTransform;
    private Vector2? _targetPosition;
    private float _rotationVelocity;
    [SerializeField] private Image _arrowImage;

    public Transform Target => _targetTransform;

    public void Init(Transform player, Transform target) {
        _player = player;
        _targetTransform = target;
        _targetPosition = null;
    }
    public void Init(Transform player, Vector2? target, bool isPlus) {
        _isPlus = isPlus;
        _player = player;
        _targetTransform = null;
        _targetPosition = target;
        if (isPlus) {
            SetIsPlusVisual();
        }
    }

    private void SetIsPlusVisual() {
        _arrowImage.color = Color.gold;
    }

    public void UpdateTarget(Transform target) {
        _targetTransform = target;
        _targetPosition = null;
    }

    /// <summary>Switches this arrow to point at a fixed world-space position.</summary>
    public void UpdateTarget(Vector2 target) {
        _targetTransform = null;
        _targetPosition = target;
    }
    public void UpdateArrow() {
        if (_player == null) return;
        if (_player == null) return;

        // Resolve the current target position from whichever source is active
        Vector2 targetPos;
        if (_targetTransform != null) targetPos = _targetTransform.position;
        else if (_targetPosition.HasValue) targetPos = _targetPosition.Value;
        else return;
        Vector2 direction = targetPos - (Vector2)_player.position;
        if (direction.sqrMagnitude < 0.0001f) return;

        float worldAngle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
        float relativeAngle = worldAngle - _player.eulerAngles.z;

        float targetAngle = -relativeAngle;
        float currentAngle = transform.localEulerAngles.z;

        float smoothAngle = Mathf.SmoothDampAngle(
            currentAngle,
            targetAngle,
            ref _rotationVelocity,
            1f
        );

        transform.localEulerAngles = new Vector3(0f, 0f, smoothAngle);
    }

}