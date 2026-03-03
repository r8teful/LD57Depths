using System;
using System.Collections;
using UnityEngine;

// Tracks the players Y position and handles the layer shaders
public class PlayerWorldLayerController : StaticInstance<PlayerWorldLayerController> {
    private WorldGenSettings _settings;

    public static event Action<int> OnPlayerWorldLayerChange;
    private int _currentPlayerLayer  = -1;
    private int _previousPlayerLayer = -1;

    private void Start() {
        if (GameSetupManager.Instance == null) return;
        _settings = GameSetupManager.Instance.WorldGenSettings;
    }

    private void FixedUpdate() {
        if (PlayerManager.Instance == null) return;
        float playerYPos = PlayerManager.Instance.transform.position.y;
        int currentIndex = _settings.GetLayerIndexFromY(playerYPos);
        if(_currentPlayerLayer != currentIndex) {
            _currentPlayerLayer = currentIndex;
            Debug.Log("ON PLAYER LAYER CHANGE: " + currentIndex);
            OnPlayerWorldLayerChange?.Invoke(currentIndex);
        }

    }
}