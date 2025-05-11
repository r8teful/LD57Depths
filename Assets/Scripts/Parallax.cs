using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Parallax : MonoBehaviour {
    [Header("References")]
    [Tooltip("The camera Transform that the background will parallax against")]
    [SerializeField] private Transform cameraTransform;

    [Header("Settings")]
    [Tooltip("Parallax multiplier (0 = static, 1 = moves with camera)")]
    [Range(0f, 2f)]
    [SerializeField] private float parallaxFactor = 0.5f;

    [Tooltip("Optional start offset")]
    [SerializeField] private Vector2 startOffset = Vector2.zero;

    private Vector2 _spriteSize;        // Size of the sprite in world units
    private Vector2 _startPosition;     // Original position of this layer
    private Vector3 _previousCamPos;    // Camera position in last frame

    private void Start() {
        // Cache the sprite size (world units) once
        var spriteBounds = GetComponent<SpriteRenderer>().bounds;
        _spriteSize = new Vector2(spriteBounds.size.x, spriteBounds.size.y);
        _startPosition = (Vector2)transform.position + startOffset;
        _previousCamPos = cameraTransform.position;
    }

    private void Update() {
        // Calculate how much the camera has moved since last frame
        Vector3 deltaCam = cameraTransform.position - _previousCamPos;

        // Apply parallax movement scaled by factor
        Vector2 parallaxMovement = new Vector2(
            deltaCam.x * parallaxFactor,
            deltaCam.y * parallaxFactor
        );
        transform.position += (Vector3)parallaxMovement;

        // Infinite wrap X
        float distX = cameraTransform.position.x * (1 - parallaxFactor);
        if (distX > _startPosition.x + _spriteSize.x)
            _startPosition.x += _spriteSize.x;
        else if (distX < _startPosition.x - _spriteSize.x)
            _startPosition.x -= _spriteSize.x;

        // Infinite wrap Y
        float distY = cameraTransform.position.y * (1 - parallaxFactor);
        if (distY > _startPosition.y + _spriteSize.y)
            _startPosition.y += _spriteSize.y;
        else if (distY < _startPosition.y - _spriteSize.y)
            _startPosition.y -= _spriteSize.y;

        // Reassign wrapped start position
        transform.position = new Vector3(
            _startPosition.x + cameraTransform.position.x * parallaxFactor,
            _startPosition.y + cameraTransform.position.y * parallaxFactor,
            transform.position.z
        );

        _previousCamPos = cameraTransform.position;
    }
}