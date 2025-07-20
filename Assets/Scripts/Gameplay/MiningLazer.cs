using DG.Tweening;
using System.Collections;
using UnityEngine;

public class MiningLazer : MiningBase {
    [Header("Raycast Gun Settings")]
    public float innerSpotAngle = 5f;
    public float outerSpotAngle = 30f;
    public override float Range { get; set; } = 10f;
    public override float DamagePerHit { get; set; } = 10f;
    public override GameObject GO => gameObject;

    public float falloffStrength = 1.5f; // Higher values = faster falloff
    public bool CanMine { get; set; } = true;
     private IToolVisual _toolVisual;
    public override IToolVisual toolVisual => _toolVisual;

    public override ToolType toolType => ToolType.Lazer;

    private void Start() {
        if(gameObject.TryGetComponent<IToolVisual>(out var c)){
            _toolVisual = c;
        } else {
            Debug.LogError("Could not find minglazerVisual on gameobject!");
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