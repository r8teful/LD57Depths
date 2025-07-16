using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Handles camera related things
public class PlayerCameraController : MonoBehaviour {
    // --- Client-Side References & Logic ---
    private Camera _playerCamera;
    private PixelPerfectCamera _playerCameraPixel;
    private void Start() {
        _playerCamera = GetComponentInChildren<Camera>();
    }
    private void OnEnable() {
        WorldVisibilityManager.OnLocalPlayerVisibilityChanged += OnPlayerVisibilityLayerChanged;
    }

    private void OnPlayerVisibilityLayerChanged(VisibilityLayerType obj) {
        float size = 11.25f;
        float time = 2f;
        switch (obj) {
            case VisibilityLayerType.Exterior:
                size = 11.25f;
                time = 2;
                break;
            case VisibilityLayerType.Interior:
                size = 9f;
                time = 1;
                break;
            default:
                break;
        }
        SetCameraZoom(size, time);
    }

    private void SetCameraZoom(float orthoSize, float time) {
        /* pixelperfect setup
        _playerCameraPixel.enabled = false;
        _playerCamera.DOOrthoSize(11.25f, 2).OnComplete(() => CameraTransitionComplete(false));
        
        // Enterior
        _playerCamera.DOOrthoSize(9, 1).OnComplete(() => CameraTransitionComplete(true));
        _playerCameraPixel.enabled = false;
         */
        _playerCamera.DOOrthoSize(orthoSize, time);
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