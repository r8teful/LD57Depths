using UnityEngine;

public class MiningDrill : MiningBase {

    public float innerSpotAngle = 5f;
    public float outerSpotAngle = 30f;
    public bool CanMine { get; private set; }
    public override GameObject GO => gameObject;
    public override float Range { get; set; } = 2f;
    public override float DamagePerHit { get; set; } = 5f;

    private IToolVisual _toolVisual;
    public override IToolVisual toolVisual => _toolVisual;

    public override ToolType toolType => ToolType.Drill;

    public override float RotationSpeed { get; set; }
    public override float KnockbackStrength { get; set; }
    public override float FalloffStrength { get; set; }

    [SerializeField] private GameObject handVisual;
    private void Awake() {
        Debug.Log("Awake called on miningDrill");
        if (gameObject.TryGetComponent<IToolVisual>(out var c)) {
            _toolVisual = c;
        } else {
            Debug.LogError("Could not find minglazerVisual on gameobject!");
        }
    }

    public override void CastRays(Vector2 pos, ToolController controller, bool isFlipped) {
        for (int i = 0; i < 5; i++) {
            Vector2 objectPos2D = new Vector2(transform.position.x, transform.position.y);
            Vector2 directionToMouse = (pos - objectPos2D).normalized;

            Vector2 rayDirection = GetConeRayDirection(directionToMouse);
            RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection, Range, LayerMask.GetMask("MiningHit"));
            if (hit.collider != null) {
                // Just assuming here that we've hit a tile, but should be fine because of the mask
                Vector2 nudgedPoint = hit.point - rayDirection * -0.1f;
                controller.CmdRequestDamageTile(new Vector3(nudgedPoint.x, nudgedPoint.y, 0), (short)DamagePerHit);
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

    //public override void ToolStart(InputManager input, ToolController controller) {
    //    base.ToolStart(input, controller);
    //    handVisual.SetActive(true);
    //    NetworkedPlayer.LocalInstance.PlayerVisuals.SetBobHand(false);
    //}
    //public override void ToolStop() {
    //    base.ToolStop();
    //    handVisual.SetActive(false);
    //    // PlayerVisual set sprite to hand
    //    NetworkedPlayer.LocalInstance.PlayerVisuals.SetBobHand(true);
    //}
}