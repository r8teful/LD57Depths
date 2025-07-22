using FishNet.Object;
using System;
using System.Collections;
using UnityEngine;
// Created and sent to the Visual part so that we know how to draw it properly
public struct MiningToolData {
    public float ToolRange;
    public int toolTier;
    // Add more as needed
}
public abstract class MiningBase : MonoBehaviour, IToolBehaviour {
    protected InputManager _inputManager;
    protected Coroutine miningRoutine;
    protected bool _isMining;
    public abstract float Range { get; set; }
    public abstract float DamagePerHit { get; set; }
    public abstract GameObject GO { get; }
    public abstract IToolVisual toolVisual { get; }
    public abstract ToolType toolType { get; }
    public ushort toolID => (ushort)toolType;

    public MiningToolData GetToolData() {
        return new MiningToolData {
            ToolRange = Range,
            toolTier = 0 //TODO
        };
    }
    protected virtual void Start() {
        Debug.Log("StartBase called on: " + toolType);
        InitVisualTool(this);
    }
    public void InitVisualTool(IToolBehaviour toolBehaviourParent) {
        toolVisual.Init(toolBehaviourParent);
    }
    private void OnEnable() {
        // Subscribe to the event to recalculate stats when a NEW upgrade is bought
        UpgradeManager.OnUpgradePurchased += HandleUpgradePurchased;
    }

    private void OnDisable() {
        UpgradeManager.OnUpgradePurchased -= HandleUpgradePurchased;
    }
    protected virtual void Update() {
        if (_isMining) {
            toolVisual.HandleVisualUpdate(_inputManager);
        }
    }
    protected virtual void HandleUpgradePurchased(UpgradeRecipeBase data) {
        if (data.type == UpgradeType.MiningRange) {
            Range = UpgradeCalculator.CalculateUpgradeIncrease(Range, data as UpgradeRecipeValue);
            Debug.Log("Increase range to " + Range);
        } else if (data.type == UpgradeType.MiningDamage) {
            DamagePerHit = UpgradeCalculator.CalculateUpgradeIncrease(DamagePerHit, data as UpgradeRecipeValue);
            Debug.Log("Increase damage to " + DamagePerHit);
        }
    }
    public virtual void ToolStart(InputManager input, ToolController controller) {
        if (miningRoutine != null) {
            Debug.LogWarning("Mining routine is still running even though it should have stopped!");
            StopCoroutine(miningRoutine);
        }
        _inputManager = input;
        _isMining = true;
        miningRoutine = StartCoroutine(MiningRoutine(controller));
        toolVisual.HandleVisualStart(controller.GetPlayerParent().PlayerVisuals);
    }
    public virtual void ToolStop(ToolController controller) {
        if (miningRoutine != null) {
            StopCoroutine(miningRoutine);
            miningRoutine = null;
            _isMining = false;
        }
        toolVisual.HandleVisualStop(controller.GetPlayerParent().PlayerVisuals);
    }
    private IEnumerator MiningRoutine(ToolController controller) {
        while (true) {
            var pos = _inputManager.GetAimInput();
            //Debug.Log(pos);
            var isFlipped = false;
            var horizontalInput = _inputManager.GetMovementInput().x;

            CastRays(pos, controller, isFlipped); // Todo determine freq here
            //LaserVisual(pos);
            yield return new WaitForSeconds(0.3f);
        }
    }
    void CastRays(Vector2 pos, ToolController controller, bool isFlipped) {
        Vector2 objectPos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 directionToMouse = (pos - objectPos2D).normalized;
        //Vector2 rayDirection = GetConeRayDirection(directionToMouse);
        Vector2 rayDirection = directionToMouse;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection, Range, LayerMask.GetMask("MiningHit"));
        if (hit.collider != null) {
            // Just assuming here that we've hit a tile, but should be fine because of the mask
            Vector2 nudgedPoint = hit.point - rayDirection * -0.1f;
            //float distance = hit.distance;
            //float falloffFactor = Mathf.Clamp01(1f - (distance / range) * falloffStrength);
            //float finalDamage = damagePerRay * falloffFactor;
            controller.CmdRequestDamageTile(new Vector3(nudgedPoint.x, nudgedPoint.y, 0), (short)DamagePerHit);
        }
    }

}