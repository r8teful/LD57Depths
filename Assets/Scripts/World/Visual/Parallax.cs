using UnityEngine;

public class Parallax : MonoBehaviour {
    [Header("Settings")]
    [Tooltip("Parallax multiplier (0 = static, 1 = moves with camera)")]
    [Range(0f, 1f)]
    [SerializeField] private float shaderParallaxFactor = 0.5f;

    [Tooltip("Optional start offset")]
    [SerializeField] private Vector2 startOffset = Vector2.zero;
    public bool infinateWrap;
    public bool yMovement;
    private Vector2 _spriteSize;        // Size of the sprite in world units
    private Vector2 _startPosition;     // Original position of this layer
    private Vector3 _previousCamPos;    // Camera position in last frame
    private float _parallaxFactor;

    private void Start() {
        // Cache the sprite size (world units) once
        _startPosition = (Vector2)transform.position + startOffset;
        _previousCamPos = Camera.main.transform.position;
        _parallaxFactor = 1f - 1f / (1f + shaderParallaxFactor);
    }

    private void Update() {
        // Calculate how much the camera has moved since last frame
        
        Vector3 deltaCam = Camera.main.transform.position - _previousCamPos;

        // Apply parallax movement scaled by factor
        Vector2 parallaxMovement = new Vector2(
            deltaCam.x * shaderParallaxFactor, yMovement ? deltaCam.y * -shaderParallaxFactor : 0
        );

        Vector2 parallaxMovement2 = new Vector2(
             Camera.main.transform.position.x * _parallaxFactor,
             yMovement ? Camera.main.transform.position.y * _parallaxFactor
              : transform.position.y
        );
        transform.position = (Vector3)parallaxMovement2;
        if (infinateWrap) {
            // Infinite wrap X
            float distX = Camera.main.transform.position.x * (1 - shaderParallaxFactor);
            if (distX > _startPosition.x + _spriteSize.x)
                _startPosition.x += _spriteSize.x;
            else if (distX < _startPosition.x - _spriteSize.x)
                _startPosition.x -= _spriteSize.x;

            // Infinite wrap Y
            float distY = Camera.main.transform.position.y * (1 - shaderParallaxFactor);
            if (distY > _startPosition.y + _spriteSize.y)
                _startPosition.y += _spriteSize.y;
            else if (distY < _startPosition.y - _spriteSize.y)
                _startPosition.y -= _spriteSize.y;

            // Reassign wrapped start position
            transform.position = new Vector3(
                _startPosition.x + Camera.main.transform.position.x * shaderParallaxFactor,
                _startPosition.y + Camera.main.transform.position.y * shaderParallaxFactor,
                transform.position.z
            );
        }

        _previousCamPos = Camera.main.transform.position;
    }
}