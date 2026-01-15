using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Handles camera related things
public class PlayerCameraController : MonoBehaviour, INetworkedPlayerModule {
    // --- Client-Side References & Logic ---
    private Camera _playerCamera;
    private PixelPerfectCamera _playerCameraPixel;
    private Tweener _zoomTween;
    private Tweener _posTween;
    public int InitializationOrder => 3;

    public void InitializeOnOwner(NetworkedPlayer playerParent) {
        _playerCamera = GetComponentInChildren<Camera>();
        WorldVisibilityManager.OnLocalPlayerVisibilityChanged += OnPlayerVisibilityLayerChanged;
    }
    
    private void OnDisable() {
        WorldVisibilityManager.OnLocalPlayerVisibilityChanged += OnPlayerVisibilityLayerChanged;
    }

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
        SetCameraZoom(size, time);
        SetCameraPos(pos,time);
    }

    private void SetCameraZoom(float size, float time) {
        if (_playerCamera == null) {
            _playerCamera = GetComponentInChildren<Camera>();
            if (_playerCamera == null) {
                Debug.LogWarning("SetCameraZoom: camera is null.");
                return;
            }
        }
        if (!_playerCamera.orthographic) {
            Debug.LogWarning("SetCameraZoom: camera is not orthographic — DOOrthoSize requires an orthographic camera.");
            return;
        }

        // If we already have an active zoom tween, smoothly update its end value & duration
        if (_zoomTween != null && _zoomTween.IsActive() && !_zoomTween.IsComplete()) {
            // ChangeEndValue updates the existing tween without snapping.
            // Passing the new duration lets you adapt speed mid-tween.
            _zoomTween = _zoomTween.ChangeEndValue(size, time);
            _zoomTween.SetEase(Ease.OutCubic).SetTarget(_playerCamera);
            return;
        }

        // Otherwise start a fresh tween from current size to target size
        _zoomTween?.Kill(); // ensure any dead/leftover tween is cleaned up
        _zoomTween = _playerCamera.DOOrthoSize(size, time)
                             .SetEase(Ease.OutCubic)
                             .SetTarget(_playerCamera);
    }

    private void SetCameraPos(Vector2 pos, float time) {
        if (_playerCamera == null) {
            Debug.LogWarning("SetCameraPos: camera is null.");
            return;
        }

        Transform t = _playerCamera.transform;
        Vector3 targetPos = new Vector3(pos.x, pos.y, t.localPosition.z);

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