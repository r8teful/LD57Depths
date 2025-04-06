using System;
using UnityEngine;

public class Resource : MonoBehaviour {
    public Tile.TileType ResourceType { get; set; }

    public float attractionRadius = 5f; // The distance at which the resource starts moving towards the player
    public float attractionSpeed = 2f;  // Speed at which the resource moves toward the player
    public float destroyDistance = 1f;  // Distance from the player at which the resource gets destroyed
    private Vector2 velocity;
    private Rigidbody2D rb;
    private PlayerController player;
   // private Renderer _renderer;
    void Start() {
        // Get the Rigidbody2D component attached to the resource
        rb = GetComponent<Rigidbody2D>();
        player = PlayerController.Instance;
        //_renderer = GetComponent<Renderer>();
        //_renderer.material = new Material(_renderer.material);
    }

    void FixedUpdate() {

        // Calculate the distance from the resource to the player
        float distanceToPlayer = Vector2.Distance(transform.position, player.transform.position);

        // If the player is within the attraction radius, move the resource towards the player
        if (distanceToPlayer <= attractionRadius) {
            Vector2 directionToPlayer = (player.transform.position - transform.position).normalized;
            velocity = directionToPlayer * attractionSpeed;

            // If the resource is close enough to the player, destroy it
            if (distanceToPlayer <= destroyDistance) {
                AudioController.Instance.PlaySound2D("popPickup", 0.5f, pitch: new AudioParams.Pitch(AudioParams.Pitch.Variation.Medium));
                Destroy(gameObject);
            }
        } else {
            // Apply drag (smooth deceleration)
           // velocity *= player.drag;
            // Apply slight downward force (simulating sinking)
           // velocity.y -= player.downwardForce * Time.deltaTime;
        }
        rb.linearVelocity = velocity;
    }
   
    internal void SetResource(Tile.TileType type) {
        ResourceType = type;
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        // Update visual etc
        switch (type) {
            case Tile.TileType.Empty:
                break;
            case Tile.TileType.Dirt:
                break;
            case Tile.TileType.Ore_Stone:
                sr.color = Color.gray;
                //_renderer.material.SetColor("_Color", Color.gray);
                break;
            case Tile.TileType.Ore_Ruby:
                sr.color = new Color(0.725f, 0.05f, 0.29f);
                //_renderer.material.SetColor("_Color", Color.gray);
                break;
            case Tile.TileType.Ore_Silver:
                sr.color = new Color(0.796f, 0.858f, 0.98f);
                break;
            case Tile.TileType.Boundary:
                break;
            case Tile.TileType.Ore_Emerald:
                sr.color = new Color(0.172f, 0.788f, 0.305f);
                break;
            case Tile.TileType.Ore_Diamond:
                sr.color = new Color(0.352f, 0.858f, 0.98f);
                break;
            default:
                break;
        }
    }
}