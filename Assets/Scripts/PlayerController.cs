using UnityEngine;

public class PlayerController : StaticInstance<PlayerController> {
    public float moveSpeed = 5f;      // Maximum movement speed
    public float acceleration = 2f;   // How quickly the player accelerates
    public float drag = 0.95f;        // Resistance to simulate water
    public float downwardForce = 1f;  // Gravity effect

    private Rigidbody2D rb;
    private Vector2 velocity;

    void Start() {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0; // Disable default gravity
    }

    void Update() {
        // Get input
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");

        Vector2 inputDirection = new Vector2(moveX, moveY).normalized;

        // Apply acceleration
        velocity += inputDirection * acceleration * Time.deltaTime;

        // Limit max speed
        velocity = Vector2.ClampMagnitude(velocity, moveSpeed);

        // Apply drag (smooth deceleration)
        velocity *= drag;

        // Apply slight downward force (simulating sinking)
        velocity.y -= downwardForce * Time.deltaTime;

    }

    void FixedUpdate() {
        rb.linearVelocity = velocity;
    }
}
