using UnityEngine;

public class PlayerController : StaticInstance<PlayerController> {
    public float moveSpeed = 5f;      // Maximum movement speed
    public float acceleration = 2f;   // How quickly the player accelerates
    public float drag = 0.95f;        // Resistance to simulate water
    public float downwardForce = 1f;  // Gravity effect
    private Animator animator;
    private string currentAnimation = "";
    private Rigidbody2D rb;
    private Vector2 velocity;
    private SpriteRenderer sprite;
    void Start() {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        rb.gravityScale = 0; // Disable default gravity
    }
    void ChangeAnimation(string animation) {
        if(currentAnimation != animation) {
            currentAnimation = animation;
            animator.CrossFade(animation,0.2f,0);
        }
    }

    void Update() {
        // Get input
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        // TODO also if we are swimming
        if (moveX != 0 || moveY != 0) { 
            ChangeAnimation("Swim");
        } else {
            ChangeAnimation("SwimIdle");
        }
        Vector2 inputDirection = new Vector2(moveX, moveY).normalized;
        if(inputDirection.x > 0) {
            sprite.flipX = false;
        } else if (inputDirection.x < 0){
            sprite.flipX = true;
        }
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
