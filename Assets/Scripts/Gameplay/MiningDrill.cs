using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiningDrill : MonoBehaviour {

    public float innerSpotAngle = 5f;
    public float outerSpotAngle = 30f;
    public bool CanMine { get; private set; }
    public int RayCount { get; set; } // How many blocks can simultaneously be mined 

    private Vector2 _visualDirection;
    float Range = 2f;//TODO
    public BuffSO Ability;
    public GameObject DrillAbilityParticles;

    public void CastRays(Vector2 pos, bool isFlipped) {
        //Debug.Log($"range:{Range} DMG: {DamagePerHit}");
        var Range = 2f;
        for (int i = 0; i < 4; i++) {
            Vector2 objectPos2D = new Vector2(transform.position.x, transform.position.y);
            Vector2 directionToMouse = (pos - objectPos2D).normalized;

            Vector2 rayDirection = GetConeRayDirection(directionToMouse,outerSpotAngle,innerSpotAngle);
            RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection, Range, LayerMask.GetMask("MiningHit"));
            if (hit.collider != null) {
                // Just assuming here that we've hit a tile, but should be fine because of the mask
                Vector2 nudgedPoint = hit.point - rayDirection * -0.1f;
                //controller.CmdRequestDamageTile(new Vector3(nudgedPoint.x, nudgedPoint.y, 0), (short)DamagePerHit);
            }
        }
    }
    Vector2 GetConeRayDirection(Vector2 baseDirection, float outerAngle, float innerAngle) {
        float randomAngle = Random.Range(-outerAngle / 2f, outerAngle / 2f); // Angle variation within outer cone
        float innerAngleThreshold = innerAngle / 2f;

        // Reduce spread near center for inner cone effect
        if (Mathf.Abs(randomAngle) < innerAngleThreshold) {
            randomAngle *= (Mathf.Abs(randomAngle) / innerAngleThreshold); // Scale angle closer to zero near center
        }

        Quaternion rotation = Quaternion.AngleAxis(randomAngle, Vector3.forward);
        return rotation * baseDirection;
    }
    private void CastRaysAbility(Vector2 pos) {
        HashSet<Vector3Int> processedCells = new HashSet<Vector3Int>();

        Vector2 origin = transform.position;
        Vector2 dir = (pos - origin).normalized;
        if (dir == Vector2.zero) return;

        float coneAngle = 50;
        // Tunables — adjust these to change accuracy/performance
        float stepAlong = 0.2f;   // How far we move along the ray per sample
        float stepAcross = 0.25f; // Sampling step across the perpendicular (smaller = more coverage)
                                  // Pre-calculate for efficiency
        Vector2 perp = new Vector2(-dir.y, dir.x);
        float halfAngleRad = coneAngle * 0.5f * Mathf.Deg2Rad;
        float cosHalfAngle = Mathf.Cos(halfAngleRad);
        float tanHalfAngle = Mathf.Tan(halfAngleRad);

        // Calculate the maximum distance we can travel along the centerline
        // so that the edges of the cone do not exceed 'Range'.
        float maxCenterlineDist = Range * cosHalfAngle;

        // Walk along the central ray of the cone, but only up to our calculated max distance
        for (float distance = 0f; distance <= maxCenterlineDist; distance += stepAlong) {


            // Calculate the cone's width at the current distance
            // This is the core change: width is now proportional to distance
            float currentHalfWidth = distance * tanHalfAngle;

            Vector2 alongPoint = origin + dir * distance;

            // Sample across the calculated width at this distance
            for (float offset = -currentHalfWidth; offset <= currentHalfWidth; offset += stepAcross) {
                Vector2 samplePoint = alongPoint + perp * offset;

                // Convert world position to the tilemap cell and process once
                Vector3Int cellPosition = WorldManager.Instance.WorldToCell((Vector3)samplePoint);
                if (processedCells.Add(cellPosition)) { // .Add returns true if the item was new
                   // controller.CmdRequestDamageTile(cellPosition, (short)DamagePerHit);
                }
            }
        }
    }
    
    // We'll just have to move this to a new mining drill script in the new way we are doing thingsS
    public IEnumerator MiningRoutineAbility() {
        while (true) {
            yield return new WaitForSeconds(1f);
            Debug.Log("SHOOOOT");
            // if (!_isMining) yield break;
            //var pos = _inputManager.GetAimWorldInput();
            var pos = Vector2.zero; 
            //Debug.Log("MiningAbilityRoutine!");
            
            //var horizontalInput = _inputManager.GetMovementInput().x;
            //CastRaysAbility(pos, controller);
            // Here we would somehow need to trigger the visual?

            //Just doing it here now BAD, because visuals is run on remote aswell and doing it here the remote wont see the actual visual 
            Vector2 objectPos2D = new Vector2(transform.position.x, transform.position.y);
            Vector2 directionToMouse = (pos - objectPos2D).normalized;

            // Calculate angle in degrees
            float angle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;

            // If you want rotation around X (your setup)
            Quaternion rotation = Quaternion.Euler(-angle, 90f, 0f);

            Instantiate(DrillAbilityParticles,transform.position, rotation, transform);
            AudioController.Instance.PlaySound2D("DrillAbility", 0.5f);
            //if (!_isUsingAbility) yield break;
        }
    }
}