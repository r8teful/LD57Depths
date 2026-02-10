using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;

public class MiningLazerVisualNew : MonoBehaviour {
    public LineRenderer lineRenderer;
    [SerializeField] private LineRenderer lineRendererChainPrefab;
    private List<LineRenderer> _chainLines = new List<LineRenderer>();
    private int _activeChainIndex = 0;
    public ParticleSystem ParticlesPrefabHit;
    public ParticleSystem _lineLazerParticleSystem;
    private ParticleSystem _hitParticleSystem;
    private PlayerManager _player;
    private AbilityInstance _abilityInstance;
    private MiningLazerNew _lazerLogic;
    private AudioSource lazerSound;
    private bool _isUsingAbility;
    private bool _hasStarted;
    private bool _chainActive;

    private void Awake() {
        SetupParticlesVisual();
        int maxChains = 5;

        for (int i = 0; i < maxChains; i++) {
            var line = Instantiate(lineRendererChainPrefab, transform);
            line.enabled = false;
            _chainLines.Add(line);
        }
    }
    public void Init(PlayerManager player, AbilityInstance ability, MiningLazerNew miningLazerNew) {
        _player = player;
        _abilityInstance = ability; // Need this for lazer length 
        _lazerLogic = miningLazerNew;
    }
    private void SetupParticlesVisual() {
        _hitParticleSystem = Instantiate(ParticlesPrefabHit);
        _hitParticleSystem.Stop();
        _lineLazerParticleSystem.Stop();
        var pMain = _lineLazerParticleSystem.main;
        pMain.simulationSpace = ParticleSystemSimulationSpace.Custom;
        pMain.customSimulationSpace = WorldManager.Instance.GetWorldRoot(); // For some reason just setting it as world doesn't work, but this does
        lineRenderer.enabled = false;
        lazerSound = AudioController.Instance.PlaySound2D("Laser", 0.0f, looping: true);
    }

    public void HandleVisualUpdate() {
        if (!_hasStarted) {
            StartVisual();
        }
        //Vector2 dir = _player.InputManager.GetDirFromPos(transform.position);
        Vector2 dir = _lazerLogic.CurrentDir; //  
        // Update visuals each frame when mining
        //bool isAbility = _player.InputManager.IsUsingAbility; // This is not really what we are wanting to know here
        // All we want to know if is the brimstone ability is active, so we can do those visuals. 
        // So maybe we just make a method in PlayerAbilities that is like
        bool isAbility = _player.PlayerAbilities.IsBrimstoneAbilityActive();


        _isUsingAbility = isAbility;
        //Debug.Log("IsAbility: " + isAbility);
        SetCorrectLaserPos(_player.InputManager.GetMovementInput().x);
        LaserVisual(dir, isAbility);
    }

    public void StartVisual() {
        _hasStarted = true;
        // Update tool data
        lazerSound.volume = 0.2f;
        // _lineWidth is 0.1 to 2, so we lerp that to get values from 1 to 0.7
        //var min = 0.1f;
        //var max = 4f;
        //laser.pitch = Mathf.Lerp(1f, 0.7f, (Mathf.Clamp(_lineWidth, min, max) - min) / (max - min));
        lazerSound.pitch = 0.7f;
        // Could also have thicker line mean more bloom -> Nice glow
        FadeInLine(lineRenderer);

        if (!_lineLazerParticleSystem.isEmitting)
            _lineLazerParticleSystem.Play();
    }

    public void EndVisual() {
        _hasStarted = false;
        _hitParticleSystem.Stop();
        lazerSound.volume = 0.0f;
        FadeOutLine(lineRenderer);
        if (_lineLazerParticleSystem.isPlaying) {
            if (_isUsingAbility) {
                _lineLazerParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            } else {
                _lineLazerParticleSystem.Stop();

            }
        }
        StopChain(); // idk
    }
    private void LaserVisual(Vector2 targetDirection, bool isAbility) {
        var range = _abilityInstance.GetEffectiveStat(StatType.MiningRange);
        Vector2 objectPos2D = new Vector2(transform.position.x, transform.position.y);
        //Debug.Log($"Target dir: {inputWorldPos} ");
        var localPos = transform.InverseTransformPoint(objectPos2D);
        if (!isAbility) {
            RaycastHit2D hit = Physics2D.Raycast(objectPos2D, targetDirection, range, LayerMask.GetMask("MiningHit"));
            if (hit.collider != null) {
                CreateLaserEffect(localPos, transform.InverseTransformPoint(hit.point), isAbility);
                _hitParticleSystem.transform.position = hit.point;
                if (!_hitParticleSystem.isPlaying)
                    _hitParticleSystem.Play();
            } else {
                // not in reange
                if (_hitParticleSystem.isPlaying)
                    _hitParticleSystem.Stop();
                CreateLaserEffect(localPos, transform.InverseTransformPoint(objectPos2D + targetDirection * range), isAbility);
            }
        } else {
            CreateLaserEffect(localPos, transform.InverseTransformPoint(objectPos2D + targetDirection * range), isAbility);
        }
    }

    private void SetCorrectLaserPos(float horizontalInput) {
        if (horizontalInput > 0.01f) {
            FlipVisual(false);
        } else if (horizontalInput < -0.01f) {
            FlipVisual(true);
        }
    }
    public void FlipVisual(bool isFlipped) {
        // I'm just as confused as you are when it comes to this bool. 
        if (!isFlipped) {
            var pos = transform.localPosition;
            pos.x = 0.5f;
            transform.localPosition = pos;
        } else {
            var pos = transform.localPosition;
            pos.x = -0.5f;
            transform.localPosition = pos;
        }
    }
    void CreateLaserEffect(Vector3 start, Vector3 end, bool isAbility) {
        var dmg = _abilityInstance.GetEffectiveStat(StatType.MiningDamage);
        var lineWidth = Mathf.Min(Mathf.Max(dmg * 0.05f,0.04f), 1f);
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth * 0.7f;
        Vector3 midpoint = (start + end) / 2;
        Vector3 direction = (end - start).normalized;
        float distance = (end - start).magnitude;

        // Position and rotate the particle system
        _lineLazerParticleSystem.transform.localPosition = midpoint;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        //_lineLazerParticleSystem.transform.rotation = Quaternion.LookRotation(direction);
        _lineLazerParticleSystem.transform.rotation = Quaternion.Euler(0, 0, angle);
        // Configure the particle system's shape
        var shape = _lineLazerParticleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
        shape.radius = distance / 2;

        // Set depending on ability
        if (isAbility) {
            var main = _lineLazerParticleSystem.main;
            main.startSpeed = new(-30, 30);
            main.startSize = 0.0f;
            var vel = _lineLazerParticleSystem.limitVelocityOverLifetime;
            vel.drag = 1;
            lineRenderer.material.SetColor("_Color", new(6, 0, 0)); // Gives it more glow
        } else {
            var main = _lineLazerParticleSystem.main;
            main.startSpeed = new(-5, 5);
            main.startSize = 0.05f;
            var vel = _lineLazerParticleSystem.limitVelocityOverLifetime;
            lineRenderer.material.SetColor("_Color", new(3, 0, 0));
            vel.drag = 16;
        }
    }
    private void FadeOutLine(LineRenderer lineRenderer) {
        //if (!lineRenderer.enabled) return; // already faded out
        //if (lineRenderer.startColor.a < 1) return; // already fading out
        DOTween.Kill(lineRenderer);
        Color2 startColor = new(lineRenderer.startColor, lineRenderer.endColor);
        var alphaStart0 = lineRenderer.startColor;
        alphaStart0.a = 0;
        var alphaEnd0 = lineRenderer.endColor;
        alphaEnd0.a = 0;
        Color2 endColor = new(alphaStart0, alphaEnd0);
        lineRenderer.DOColor(startColor, endColor, 0.2f).OnComplete(() => lineRenderer.enabled = false);
    }
    private void FadeInLine(LineRenderer lineRenderer) {
        //if (lineRenderer.enabled) return; // already faded in
        //if (lineRenderer.startColor.a > 0) return; // already fading in
        DOTween.Kill(lineRenderer);
        Color2 startColor = new(lineRenderer.startColor, lineRenderer.endColor);
        var alphaStart1 = lineRenderer.startColor;
        alphaStart1.a = 1;
        var alphaEnd1 = lineRenderer.endColor;
        alphaEnd1.a = 1;
        Color2 endColor = new(alphaStart1, alphaEnd1);
        lineRenderer.DOColor(startColor, endColor, 0.07f).OnComplete(() => lineRenderer.enabled = true);
    }

    void CreateLaserEffectChain(Vector3 start, Vector3 end) {
        var dmg = _abilityInstance.GetEffectiveStat(StatType.MiningDamage);
        var lineWidth = Mathf.Min(Mathf.Max(dmg * 0.05f, 0.04f), 1f);
        lineRendererChainPrefab.SetPosition(0, start);
        lineRendererChainPrefab.SetPosition(1, end);
        lineRendererChainPrefab.startWidth = lineWidth;
        lineRendererChainPrefab.endWidth = lineWidth * 0.7f;
        lineRenderer.material.SetColor("_Color", new(3, 0, 0));
    }

    internal void StopChain() {
        if (_chainActive) {
            foreach (var line in _chainLines) {
                FadeOutLine(line);
            }
            _chainActive = false;
        }
    }

    internal void StartChain() {
        if (!_chainActive) {
            _activeChainIndex = 0;
            _chainActive = true;
        }
    }
    // Call this to update a SPECIFIC link in the chain
    internal void DrawSpecificChain(int index, Vector2 start, Vector2 target) {
        if (index >= _chainLines.Count) return;

        var line = _chainLines[index];
        line.enabled = true;
        FadeInLine(line);
        Vector2 localStart = transform.InverseTransformPoint(start);
        Vector2 localEnd = transform.InverseTransformPoint(target);

        CreateLaserEffectChain(line, localStart, localEnd);
    }

    // Call this at the end to hide lines we didn't use this frame
    internal void HideChainsFromIndex(int index) {
        for (int i = index; i < _chainLines.Count; i++) {
            _chainLines[i].enabled = false;
        }
    }

    void CreateLaserEffectChain(LineRenderer lr, Vector3 start, Vector3 end) {
        var dmg = _abilityInstance.GetEffectiveStat(StatType.MiningDamage);
        var lineWidth = Mathf.Min(Mathf.Max(dmg * 0.05f, 0.04f), 1f);
        lr.positionCount = 2; 
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);

        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth * 0.7f;

        lr.material.SetColor("_Color", new Color(3, 0, 0));
    }
}