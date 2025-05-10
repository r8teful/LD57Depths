using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiningGun : MonoBehaviour {
    [Header("Raycast Gun Settings")]
    public float innerSpotAngle = 5f;
    public float outerSpotAngle = 30f;
    public float range = 10f;
    public float falloffStrength = 1.5f; // Higher values = faster falloff
    public float frequency = 10f;        // Rays per second
    public float damagePerRay = 10f;     // Base damage per ray
    public bool CanShoot { get; set; }
    private float timer = 0f;
    private AudioSource laser;
    [Header("Visual Settings")]
    public float startWidth;
    public float endWidth;
    public Color rayColor;
    public LineRenderer lineRendererPrefab;  // Assign in Inspector
    public ParticleSystem Particles;
    private List<LineRenderer> activeLineRenderers = new List<LineRenderer>();

    private void Awake() {
        UpgradeManager.UpgradeBought += OnUpgraded;
    }
    private void OnDestroy() {
        UpgradeManager.UpgradeBought -= OnUpgraded;
    }
    private void Start() {
        laser = AudioController.Instance.PlaySound2D("Laser", 0.0f, looping: true);
    }
    void Update() {
        if (!CanShoot) return;
        if (Input.GetMouseButton(0)) // Left mouse button held down
        {
            LaserVisual();
            timer += Time.deltaTime;
            if (timer >= 1f / frequency) {
                timer -= 1f / frequency; // Reset timer, but keep fractional remainder for accuracy
                CastRays();
            }
        } else {
            if (Particles.isPlaying) { 
                Particles.Stop();
                laser.volume = 0.0f;
            }
        }
    }
    private void LaserVisual() {
        if (!Particles.isPlaying) { 
            Particles.Play();
            laser.volume = 0.5f;
        }
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;
        Vector2 directionToMouse = (mouseWorldPosition - transform.position).normalized;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, directionToMouse, range);
        CreateLaserEffect(transform.position, hit.point);
        Particles.transform.position = hit.point;
    }

    void CastRays() {
        Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPosition.z = 0f;
        Vector2 directionToMouse = (mouseWorldPosition - transform.position).normalized;

        int numRays = 5;
        ClearPreviousRays(); // Clear old rays before shooting new ones

        for (int i = 0; i < numRays; i++) {
            //Vector2 rayDirection = GetConeRayDirection(directionToMouse);
            Vector2 rayDirection = directionToMouse;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection, range);

            if (hit.collider != null) {
                //TileScript hitTile = hit.collider.GetComponent<TileScript>();
                TileSO hitTile = null; // TODO obviously
                if (hitTile != null) {
                    float distance = hit.distance;
                    float falloffFactor = Mathf.Clamp01(1f - (distance / range) * falloffStrength);
                    float finalDamage = damagePerRay * falloffFactor;

                    //GridManager.Instance.DamageTileAtGridPosition(hitTile.gridPosition, finalDamage);
                    //CreateLaserEffect(transform.position, hit.point);
                } else {
                   // CreateLaserEffect(transform.position, hit.point);
                }
            } else {
               //CreateLaserEffect(transform.position, transform.position + (Vector3)rayDirection * range);
            }
        }
    }
    void CreateLaserEffect(Vector3 start, Vector3 end) {
        LineRenderer line = Instantiate(lineRendererPrefab, transform);
        line.SetPosition(0, start);
        line.SetPosition(1, end);
        line.startColor = rayColor;
        line.endColor = new Color(rayColor.r, rayColor.g, rayColor.b, 0.2f); // Fades out at the end
        line.startWidth = startWidth;
        line.endWidth = endWidth;
        activeLineRenderers.Add(line);

        StartCoroutine(FadeAndDestroy(line));
    }
    IEnumerator FadeAndDestroy(LineRenderer line) {
        float duration = 0.2f;
        float elapsedTime = 0;
        while (elapsedTime < duration) {
            if (line == null) yield break; // Exit if the line has been destroyed
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            if (line != null) {
                Color startColor = line.startColor;
                Color endColor = line.endColor;
                startColor.a = alpha;
                endColor.a = alpha;
                line.startColor = startColor;
                line.endColor = endColor;
            }
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        if (line != null)
            Destroy(line.gameObject);
    }
    void ClearPreviousRays() {
        foreach (var line in activeLineRenderers) {
            if (line != null) Destroy(line.gameObject);
        }
        activeLineRenderers.Clear();
    }
    public void OnUpgraded(UpgradeType t) {
        if(t == UpgradeType.MiningDamange) {
            damagePerRay = UpgradeManager.Instance.GetUpgradeValue(UpgradeType.MiningDamange);
        } else if (t == UpgradeType.MiningSpeed) {
            frequency = UpgradeManager.Instance.GetUpgradeValue(UpgradeType.MiningSpeed);
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

    internal void Flip(bool facingLeft) {
        Vector3 position = transform.localPosition;
        if (facingLeft) {
            position.x = -Mathf.Abs(position.x); // Ensure it moves to the left
        } else {
            position.x = Mathf.Abs(position.x); // Ensure it moves to the right
        }
        transform.localPosition = position;
    }
}