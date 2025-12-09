using System.Collections;
using UnityEngine;

public class MiningRPG : MiningBase {
    [SerializeField] private Transform spawnPos;
    public override ToolType ToolType => ToolType.RPG;
    public float ExplosionVelocity;
    public override BuffSO AbilityData => Ability;
    private Vector2 _visualDirection;

    public override object VisualData => _visualDirection;

    public BuffSO Ability;
    public override void CastRays(Vector2 pos, ToolController controller, bool isFlipped) {
        Vector2 toolPosition = transform.position;
        Vector2 targetDirection = (pos - toolPosition).normalized;

        // Calculate the angle in degrees from the target direction
        float angle = Mathf.Atan2(targetDirection.y, targetDirection.x) * Mathf.Rad2Deg;
        // Create a quaternion for the rotation (rotate around Z-axis for 2D)
        Quaternion rotation = Quaternion.Euler(0f, 0f, angle);
        // Instantiate the projectile with the calculated rotation
        AudioController.Instance.PlaySound2D("RPGShoot",1);
        Instantiate(App.ResourceSystem.GetPrefab<RPGProjectile>("RPGProjectile"), spawnPos.position, rotation).Init(targetDirection * ExplosionVelocity,controller);
    }
    public override IEnumerator MiningRoutine(ToolController controller) {
        while (true) {
            yield return new WaitForSeconds(1f); // Charge up time
            if (!_isMining) yield break;

            var pos = _inputManager.GetAimWorldInput();
            //Debug.Log(pos);
            var isFlipped = false;
            var horizontalInput = _inputManager.GetMovementInput().x;

            CastRays(pos, controller, isFlipped); 
        } 
    }

    public override IEnumerator MiningRoutineAbility(ToolController controller) {
        throw new System.NotImplementedException();
    }

    public override void OwnerUpdate() {
        _visualDirection = _inputManager.GetAimWorldInput(transform);
    }
}