using FishNet.Object;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour
{
    private Camera _playerCamera;
    private Vector2 currentInput;
    [SerializeField] private Rigidbody2D rb;
    
    public override void OnStartClient() {
        base.OnStartClient();
        if (base.IsOwner) // Check if this NetworkObject is owned by the local client
        {
            Debug.Log("We are the owner!");
            _playerCamera = Camera.main;
            _playerCamera.transform.SetParent(transform);
            _playerCamera.transform.localPosition = new Vector3(0, 0, -10);
            // Enable input, camera controls ONLY for the local player
            // Example: GetComponent<PlayerInputHandler>().enabled = true;
            // Example: playerCamera.SetActive(true);
        } else {
            // Disable controls for remote players on this client
            Debug.Log("We are NOT the owner!");
            GetComponent<PlayerMovement>().enabled = false;
            // Example: GetComponent<PlayerInputHandler>().enabled = false;
        }

        // Try to find the WorldGenerator - might need adjustment based on your scene setup
        // worldGenerator = FindObjectOfType<WorldGenerator>();
        // if (worldGenerator == null) {
        //     Debug.LogError("PlayerController could not find WorldGenerator!");
        // }
    }
    
    public float moveSpeed = 5f;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update() {
        // Get raw input (no smoothing)
        currentInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
    }

    private void FixedUpdate() {
        // Move the player
        rb.linearVelocity = currentInput * moveSpeed;
    }
}
