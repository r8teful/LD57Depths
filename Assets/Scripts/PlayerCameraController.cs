using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Handles camera related things
public class PlayerCameraController : MonoBehaviour {
    // --- Client-Side References & Logic ---
    private Camera _playerMainCamera;
    [SerializeField] private Camera _playerCameraRest;
    private PixelPerfectCamera _playerCameraPixel;
    private Tweener _zoomTween;
    private Tweener _posTween;

    private Vector3 velocity;
    private Transform playerTarget;
    private bool shouldShake;
    private Tweener shakeTween;

    public bool IsMoving => _posTween.IsActive() || !_zoomTween.IsComplete();

    public void Awake() {
        _playerMainCamera = GetComponent<Camera>();
        PlayerLayerController.OnPlayerVisibilityChanged += OnPlayerVisibilityLayerChanged;
     }

    void Update() {
        if (shouldShake && shakeTween == null) {
            StartShake();
        } else if (!shouldShake && shakeTween != null) {
            StopShake();
        }
    }

    private void OnDisable() {
        PlayerLayerController.OnPlayerVisibilityChanged -= OnPlayerVisibilityLayerChanged;
    }
    /* This doesn't work because we are a child of the player, but its a  lot nicer
    private void LateUpdate() {
        if(playerTarget == null) {
            if (PlayerManager.Instance != null) playerTarget = PlayerManager.Instance.gameObject.transform;
            return;
        }
        Vector3 desiredPosition = playerTarget.position + new Vector3(0f,0f, -10f);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, 0.1f);
    }
     */
    private void OnPlayerVisibilityLayerChanged(VisibilityLayerType layerType) {
        Debug.Log("OnPlayerVisibilityLayerChanged called with: " + layerType);
        float size = 11.25f;
        float time = 2f;
        Vector2 pos = Vector2.zero;
        switch (layerType) {
            case VisibilityLayerType.Exterior:
                size = 11.25f;
                time = 2;
                break;
            case VisibilityLayerType.Interior:
                size = 9f;
                time = 1;
                pos = new(0,2.5f);
                break;
            default:
                break;
        }

        time *= 0.8f;
        SetCameraLayerMask(layerType);
        SetCameraZoom(size, time);
        SetCameraPos(pos,time);
    }

    private void SetCameraLayerMask(VisibilityLayerType layerType) {
        // This simply toggles so we got to hope it never does the same twice
        int mask = LayerMask.GetMask("Default", "NoPlayerCollisions", "MiningHit", "InteractablesExterior");
        //_playerCameraRest.cullingMask ^= mask;
        if (layerType == VisibilityLayerType.Interior) {
            // remove mask (outside stuff)
            _playerCameraRest.cullingMask &= ~mask;
        } else {
            // show outside interactables again
            _playerCameraRest.cullingMask |= mask;
        }
    }

    private void SetCameraZoom(float size, float time) {
        if (_playerMainCamera == null) {
            _playerMainCamera = GetComponentInChildren<Camera>();
            if (_playerMainCamera == null) {
                Debug.LogWarning("SetCameraZoom: camera is null.");
                return;
            }
        }
        if (!_playerMainCamera.orthographic) {
            Debug.LogWarning("SetCameraZoom: camera is not orthographic — DOOrthoSize requires an orthographic camera.");
            return;
        }

        // If we already have an active zoom tween, smoothly update its end value & duration
        if (_zoomTween != null && _zoomTween.IsActive() && !_zoomTween.IsComplete()) {
            // ChangeEndValue updates the existing tween without snapping.
            // Passing the new duration lets you adapt speed mid-tween.
            _zoomTween = _zoomTween.ChangeEndValue(size, time);
            _zoomTween.SetEase(Ease.OutCubic).SetTarget(_playerMainCamera);
            return;
        }

        // Otherwise start a fresh tween from current size to target size
        _zoomTween?.Kill(); // ensure any dead/leftover tween is cleaned up
        _zoomTween = _playerMainCamera.DOOrthoSize(size, time)
                             .SetEase(Ease.OutCubic)
                             .SetTarget(_playerMainCamera);
    }
    public void SetCameraPosRelative(Vector2 globalPos,float time) {
       var p =  _playerMainCamera.transform.parent.InverseTransformPoint(globalPos);
        SetCameraPos(p, time);
    }

    public void SetCameraPos(Vector2 localPos, float time) {
        if (_playerMainCamera == null) {
            Debug.LogWarning("SetCameraPos: camera is null.");
            return;
        }

        Transform t = _playerMainCamera.transform;
        Vector3 targetPos = new Vector3(localPos.x, localPos.y, t.localPosition.z);

        // If we already have an active position tween, update its end value & duration
        if (_posTween != null && _posTween.IsActive() && !_posTween.IsComplete()) {
            _posTween = _posTween.ChangeEndValue(targetPos, time);
            _posTween.SetEase(Ease.OutCubic).SetTarget(t);
            return;
        }

        // Otherwise start a new one
        _posTween?.Kill();
        _posTween = t.DOLocalMove(targetPos, time)
                       .SetEase(Ease.OutCubic)
                       .SetTarget(t);
    }
    public void Shake(float length = 0.2f) {
        // Note to self: Higher vibraro makes it more subtle
        _playerMainCamera.DOShakePosition(length,0.1f,40);
    }
    void StartShake() {
        shakeTween = _playerMainCamera
            .DOShakePosition(0.5f, 0.1f, 40, 90,false)
            .SetLoops(-1, LoopType.Restart)
            .SetEase(Ease.Linear);
    }

    void StopShake() {
        shakeTween.Kill();
        shakeTween = null;
        //_playerMainCamera.transform.localPosition = Vector3.zero;
    }

    public void ShakeToggle(bool active) {
        shouldShake = active;
    }
    private TweenCallback CameraTransitionComplete(bool isEnterior) {
        if (isEnterior) {
            _playerCameraPixel.assetsPPU = 10;
        } else {
            _playerCameraPixel.assetsPPU = 8;
        }
        _playerCameraPixel.enabled = true;
        return null;
    }
}