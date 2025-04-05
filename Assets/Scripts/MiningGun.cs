using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class MiningGun : MonoBehaviour {
    [Header("Raycast Gun Settings")]
    public float innerSpotAngle = 5f;
    public float outerSpotAngle = 30f;
    public float range = 10f;
    public float falloffStrength = 1.5f; // Higher values = faster falloff
    public float frequency = 10f;        // Rays per second
    public float damagePerRay = 10f;     // Base damage per ray

    private float timer = 0f;


    void Update() {
        if (Input.GetMouseButton(0)) // Left mouse button held down
        {
            timer += Time.deltaTime;
            if (timer >= 1f / frequency) {
                timer -= 1f / frequency; // Reset timer, but keep fractional remainder for accuracy
                CastRays();
            }
        }
    }

    void CastRays() {
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition); 
        mouseWorldPosition.z = 0f;  // Set z to 0 to keep it on the same plane as the player
        Vector2 directionToMouse = (mouseWorldPosition - transform.position).normalized;
        // Number of rays to cast - adjust for performance and spread
        int numRays = 5; // Increased number for better cone coverage
        
        for (int i = 0; i < numRays; i++) {
            // Calculate ray direction with cone spread
            Vector2 rayDirection = GetConeRayDirection(directionToMouse);

            // Cast ray 
            RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection, range);
            //RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToMouse, range);
            //Debug.Log($"shooting ray from {transform.position} in dir {rayDirection}");
            if (hit.collider != null) {
                Tile hitTile = hit.collider.GetComponent<Tile>();
                if (hitTile != null) {
                    // Calculate distance falloff
                    float distance = hit.distance;
                    float falloffFactor = Mathf.Clamp01(1f - (distance / range) * falloffStrength); 

                    float finalDamage = damagePerRay * falloffFactor;

                    // Get Grid Position and Apply Damage through GridManager
                    //Vector2Int gridPosition = GridManager.Instance.GetGridPositionFromWorldPosition(hit.point);
                    Debug.Log($"Hit: {hitTile.gridPosition} damage: {finalDamage}");
                    GridManager.Instance.DamageTileAtGridPosition(hitTile.gridPosition, finalDamage);

                    // Optional: Visualize rays for debugging - comment out in final game
                    Debug.DrawRay(transform.position, rayDirection * distance, Color.yellow, 1);
                } else {
                    // Optional: Debug for hitting non-tile colliders
                    Debug.DrawRay(transform.position, rayDirection * hit.distance, Color.gray, 1);
                }
            } else {
                // Optional: Visualize rays that didn't hit anything
                Debug.DrawRay(transform.position, rayDirection * range, Color.blue, 1);
            }
        }
    }

    Vector2 GetConeRayDirection(Vector2 baseDirection) {
        float randomAngle = Random.Range(-outerSpotAngle / 2f, outerSpotAngle / 2f); // Angle variation within outer cone
        float innerAngleThreshold = innerSpotAngle / 2f;

        // Reduce spread near center for inner cone effect
        if (Mathf.Abs(randomAngle) < innerAngleThreshold) {
            randomAngle *= (Mathf.Abs(randomAngle) / innerAngleThreshold); // Scale angle closer to zero near center
        }

        Quaternion rotation = Quaternion.AngleAxis(randomAngle, Vector3.forward);
        return rotation * baseDirection;
    }
}