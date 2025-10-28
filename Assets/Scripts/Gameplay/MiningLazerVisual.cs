using DG.Tweening;
using System.Collections;
using UnityEngine;

public class MiningLazerVisual : MonoBehaviour, IToolVisual {

    public LineRenderer lineRenderer;
    public ParticleSystem ParticlesPrefabHit;
    public ParticleSystem _lineLazerParticleSystem;
    private ParticleSystem _hitParticleSystem;

    private Vector2 _inputPrev;
    private Vector2 _inputCurrent;
    private AudioSource laser;

    private float _range;
    private float _lineWidth;
    private Coroutine _currentRoutine;
    private PlayerStatsManager _localPlayerStats;
    private bool _isOwner;
    private Vector2 _nextInput;
    private MiningToolData _cachedToolData;

    public (Sprite, Sprite) BackSprites => (null,null);

    // All clients run this
    public void Init(bool isOwner, NetworkedPlayer player) {
        _localPlayerStats = player.PlayerStats;
        _isOwner = isOwner;
        if (_localPlayerStats.IsInitialized) {
            // If stats are already ready (e.g., late join), grab them immediately.
            InitializeWithCurrentStats(_localPlayerStats);
        }
        player.PlayerVisuals.OnToolInitBack(this); // This should be automatic but eh
        SetupParticlesVisual();
    }
    private void InitializeWithCurrentStats(PlayerStatsManager pStats) {
        _cachedToolData = pStats.GetToolData();
    }
    // Called once every frame for owner, and once every 0.4s for non owners
    public void UpdateVisual(object inputData, InputManager inputMan) {
        if (_isOwner) {
            if (inputMan == null) Debug.LogError("Need to set InputManager!");
            HandleVisualUpdate(inputMan);
        } else {
            if(inputData is Vector2 vector) {
                // Update the target position. update will handle the smooth movement.
                _nextInput = vector;
            } else {
                Debug.LogWarning($"Inputdata is not a vector2!");
            }
        }
    }

    public void StartVisual() {
        // Update tool data
        if (!_localPlayerStats) return;
        _cachedToolData = _localPlayerStats.GetToolData();
        _range = _cachedToolData.ToolRange;
        _lineWidth = _cachedToolData.ToolWidth;
        laser.volume = 0.2f;
        // _lineWidth is 0.1 to 2, so we lerp that to get values from 1 to 0.7
        laser.pitch = Mathf.Lerp(1f, 0.7f, (Mathf.Clamp(_lineWidth, 0.1f, 2) - 0.1f) / (2f - 0.1f));
        // Could also have thicker line mean more bloom -> Nice glow
        FadeInLine(lineRenderer);
    }

    public void StopVisual() {
        _hitParticleSystem.Stop();
        laser.volume = 0.0f;
        FadeOutLine(lineRenderer);
        if (_lineLazerParticleSystem.isPlaying)
            _lineLazerParticleSystem.Stop();
    }
    private void SetupParticlesVisual() {
        _hitParticleSystem = Instantiate(ParticlesPrefabHit);
        _hitParticleSystem.Stop();
        _lineLazerParticleSystem.Stop();
        var pMain = _lineLazerParticleSystem.main;
        pMain.simulationSpace = ParticleSystemSimulationSpace.Custom;
        pMain.customSimulationSpace = WorldManager.Instance.GetWorldRoot(); // For some reason just setting it as world doesn't work, but this does
        lineRenderer.enabled = false;
        laser = AudioController.Instance.PlaySound2D("Laser", 0.0f, looping: true);
    }

    // interpolation loop for remote clients
    private void Update() {
        if (_isOwner) 
            return;
        _inputCurrent = _nextInput;
        if (_inputCurrent != _inputPrev) {
            if (_currentRoutine != null) {
                StopCoroutine(_currentRoutine);
            }
            _currentRoutine = StartCoroutine(SmoothInterpolate(_inputPrev, _inputCurrent));    
        }
    }

    public void HandleVisualUpdate(InputManager inputManager) {
        // Update visuals each frame when mining
        var pos = inputManager.GetAimWorldInput();

        bool isAbility = inputManager.IsUsingAbility;
        SetCorrectLaserPos(inputManager.GetMovementInput().x);
        LaserVisual(pos, isAbility);
    }
    private void FadeOutLine(LineRenderer lineRenderer) {
        Color2 startColor = new(lineRenderer.startColor, lineRenderer.endColor);
        var alphaStart0 = lineRenderer.startColor;
        alphaStart0.a = 0;
        var alphaEnd0 = lineRenderer.endColor;
        alphaEnd0.a = 0;
        Color2 endColor = new(alphaStart0, alphaEnd0);
        lineRenderer.DOColor(startColor, endColor, 0.2f).OnComplete(() => lineRenderer.enabled = false);
    }
    private void FadeInLine(LineRenderer lineRenderer) {
        Color2 startColor = new(lineRenderer.startColor, lineRenderer.endColor);
        var alphaStart1 = lineRenderer.startColor;
        alphaStart1.a = 1;
        var alphaEnd1 = lineRenderer.endColor;
        alphaEnd1.a = 1;
        Color2 endColor = new(alphaStart1, alphaEnd1);
        lineRenderer.DOColor(startColor, endColor, 0.07f).OnComplete(() => lineRenderer.enabled = true);
    }

    private void SetCorrectLaserPos(float horizontalInput) {
        if (horizontalInput > 0.01f) {
            var pos = transform.localPosition;
            pos.x = 0.25f;
            transform.localPosition = pos;
        } else if (horizontalInput < -0.01f) {
            var pos = transform.localPosition;
            pos.x = -0.25f;
            transform.localPosition = pos;
        }
    }

    private void LaserVisual(Vector2 inputWorldPos, bool isAbility) {
        if (!_lineLazerParticleSystem.isPlaying) {
            _lineLazerParticleSystem.Play();
        }
        Vector2 objectPos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 targetDirection = (inputWorldPos - objectPos2D).normalized;

        var localPos = transform.InverseTransformPoint(objectPos2D);
        if (!isAbility) {
            RaycastHit2D hit = Physics2D.Raycast(objectPos2D, targetDirection, _range, LayerMask.GetMask("MiningHit"));
            if (hit.collider != null) {
                CreateLaserEffect(localPos, transform.InverseTransformPoint(hit.point), isAbility);
                _hitParticleSystem.transform.position = hit.point;
                if (!_hitParticleSystem.isPlaying)
                    _hitParticleSystem.Play();
            } else {
                // not in reange
                if (_hitParticleSystem.isPlaying)
                    _hitParticleSystem.Stop();
                CreateLaserEffect(localPos, transform.InverseTransformPoint(objectPos2D + targetDirection * _range), isAbility);
            }
        } else {
                CreateLaserEffect(localPos, transform.InverseTransformPoint(objectPos2D + targetDirection * _range), isAbility);
        }
    }


    void CreateLaserEffect(Vector3 start, Vector3 end, bool isAbility) {
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
        lineRenderer.startWidth = _lineWidth;
        lineRenderer.endWidth = _lineWidth * 0.7f;
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
            main.startSpeed = new(-10, 10);
            main.startSize = 0.1f;
            var vel = _lineLazerParticleSystem.limitVelocityOverLifetime;
            vel.drag = 4;
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
    private IEnumerator SmoothInterpolate(Vector2 from, Vector2 to) {
        float duration = 0.4f; // This should match the syncvar update frequency
        float elapsed = 0f;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            Vector2 lerped = Vector2.Lerp(from, to, elapsed / duration);
            _inputPrev = lerped; // This makes sense right?
            LaserVisual(lerped,false); // TODO this will not work we'll have to sync if the're using an ability..
            yield return null;
        }
        LaserVisual(to,false);
        _currentRoutine = null;// Cleanup
    }
}