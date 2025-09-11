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
public abstract class MiningBase : NetworkBehaviour, IToolBehaviour {
    protected InputManager _inputManager;
    protected Coroutine miningRoutine;
    protected bool _isMining;
    public abstract float Range { get; set; }
    public abstract float DamagePerHit { get; set; }
    public abstract GameObject GO { get; }
    public abstract IToolVisual toolVisual { get; }
    public abstract ToolType toolType { get; }
    public ushort toolID => (ushort)toolType;
    private PlayerStatsManager _localPlayerStats; // The script is attached to a player, this is that players stats, could be our stats, or a remove clients stats

    private void InitializeWithCurrentStats() {
        var pStats = NetworkedPlayer.LocalInstance.PlayerStats;
        Range = pStats.GetStat(StatType.MiningRange);
        DamagePerHit = pStats.GetStat(StatType.MiningDamage);
    }
    private void OnStatChanged(StatType type, float newV) {
        if(type == StatType.MiningDamage) {
            DamagePerHit = newV;
        }
        if (type == StatType.MiningRange) {
            Range = newV;
        }
        if (type == StatType.MiningHandling) {
            // TODO
        }
        Debug.Log($"New upgrade {type} is: " + newV);
    }
    public override void OnStartClient() {
        base.OnStartClient();
        Debug.Log("StartBase called on: " + toolType);
        InitializeWithCurrentStats();
        InitVisualTool(this);
        if (NetworkedPlayersManager.Instance.TryGetPlayer(OwnerId, out var player)) {
            _localPlayerStats = player.PlayerStats;
            _localPlayerStats.OnStatChanged += OnPlayerStatsChange;
        }
    }

    private void OnPlayerStatsChange(StatType stat, float newV) {
        if(stat == StatType.MiningRange) {
            Range = newV;
        }
    }

    public override void OnStopClient() {
        base.OnStartClient();
        if(_localPlayerStats != null) {
            _localPlayerStats.OnStatChanged -= OnPlayerStatsChange;
        }
    }
    public MiningToolData GetToolData() {
        return new MiningToolData {
            ToolRange = Range,
            toolTier = 0 //TODO
        };
    }
    public void InitVisualTool(IToolBehaviour toolBehaviourParent) {
        toolVisual.Init(toolBehaviourParent);
    }
    protected virtual void Update() {
        if (_isMining) {
            toolVisual.HandleVisualUpdate(_inputManager);
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
            var pos = _inputManager.GetAimWorldInput();
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