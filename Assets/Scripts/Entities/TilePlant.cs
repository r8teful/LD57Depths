using UnityEngine;
using System.Collections;
using FishNet.Object;
using UnityEngine.Tilemaps;
using System;

// Todo
public class TilePlant : ExteriorObject, ITileChangeReactor {

    [SerializeField] private float groundCheckDistance = 0.6f;
    [SerializeField] private LayerMask groundLayer;

    private bool isGrounded = true; // Server state
    private Rigidbody2D rb;


    
    public void OnTileChangedNearby(Vector3Int cellPosition, int newTileID) {
        if (newTileID == 0) {
            if(cellPosition == transform.position) {
                Despawn();
            }
        }
    }

    public override void OnStartServer() {
        base.OnStartServer();
        rb = GetComponent<Rigidbody2D>();
        if (rb) rb.bodyType = RigidbodyType2D.Kinematic; // Start kinematic
       // CheckGroundedState(); // Initial check
    }
/*
    // --- Called by EntityManager via Interface ---
    [Server] // Ensure server execution
    public void OnTileChangedNearby(Vector3Int cellPosition, int newTileID) {
        // Check if the change happened directly below us
        if (newTileID == 0) {
            if (cellPosition == transform.position) {
                // The tile directly below us changed! Re-check if we are grounded.
                CheckGroundedState();
            }
        }
    }*/

    [Server]
    private void CheckGroundedState() {
        // Raycast down slightly further than foot position
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        bool nowGrounded = hit.collider != null;

        if (isGrounded && !nowGrounded) {
            // Was grounded, now isn't -> Start falling!
            isGrounded = false;
            Debug.Log($"{gameObject.name} lost ground, starting to fall!");
            if (rb) rb.bodyType = RigidbodyType2D.Dynamic; // Enable physics
                                                           // Client visual update handled by NetworkTransform syncing Rigidbody
        } else if (!isGrounded && nowGrounded) {
            // Was falling, now grounded -> Stop falling
            isGrounded = true;
            Debug.Log($"{gameObject.name} landed!");
            if (rb) rb.bodyType = RigidbodyType2D.Kinematic; // Disable physics, maybe reset velocity
                                                             // Potentially snap to ground slightly? rb.position = ... hit.point ... ?
        }
        // Else no change in grounded state
    }
   
}