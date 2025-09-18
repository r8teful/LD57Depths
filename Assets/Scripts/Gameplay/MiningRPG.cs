using System.Collections;
using UnityEngine;

public class MiningRPG : MiningBase {

    public override ToolType ToolType => ToolType.RPG;
    public float ExplosionStrength;
    public override ToolAbilityBaseSO AbilityData => Ability;

    public ToolAbilityBaseSO Ability;
    public override void CastRays(Vector2 pos, ToolController controller, bool isFlipped) {
        Vector2 toolPosition = transform.position;
        Vector2 targetDirection = (pos - toolPosition).normalized;

        // Shoot out projectile in direction, projectile will handle damage and other calculations...
        Instantiate(App.ResourceSystem.GetPrefab<RPGProjectile>("RPGProjectile")).Init(targetDirection * ExplosionStrength,controller);
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
}