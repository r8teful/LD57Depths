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
    private IToolBehaviour _toolBehaviour;
    private Coroutine _currentRoutine;

    public void Init(IToolBehaviour parent) {
        _toolBehaviour = parent;
    }
    private void Start() {
        _hitParticleSystem = Instantiate(ParticlesPrefabHit);
        _hitParticleSystem.Stop();
        _lineLazerParticleSystem.Stop();
        var pMain = _lineLazerParticleSystem.main;
        pMain.simulationSpace = ParticleSystemSimulationSpace.Custom;
        pMain.customSimulationSpace = WorldManager.Instance.GetWorldRoot(); // For some reason just setting it as world doesn't work, but this does
        lineRenderer.enabled = false;
        laser = AudioController.Instance.PlaySound2D("Laser", 0.0f, looping: true);
    }
    public void HandleVisualStart(PlayerVisualHandler playerVisualHandler) {
        // Update the stats, this is ugly but it works I think, we could add an event when we change it and only then update it, but this also works
        _range = _toolBehaviour.GetToolData().ToolRange;
        laser.volume = 0.2f;
        FadeInLine(lineRenderer);
    }

    public void HandleVisualStop(PlayerVisualHandler playerVisualHandler) {
        HandleLaserVisualStop();
    }

    public void HandleVisualUpdate(Vector2 inputDir, InputManager inputManager) {
        // Update visuals each frame when mining
        //var pos = inputManager.GetAimWorldInput();
        var pos = inputDir;
        Debug.Log(pos);
        SetCorrectLaserPos(inputManager.GetMovementInput().x);
        LaserVisual(pos);
    }
    private void HandleLaserVisualStop() {
        _hitParticleSystem.Stop();
        laser.volume = 0.0f;
        FadeOutLine(lineRenderer);
        if (_lineLazerParticleSystem.isPlaying)
            _lineLazerParticleSystem.Stop();
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

    private void LaserVisual(Vector2 pos) {
        if (!_lineLazerParticleSystem.isPlaying)
            _lineLazerParticleSystem.Play();
        Vector2 objectPos2D = new Vector2(transform.position.x, transform.position.y);
        //Vector2 directionToMouse = (pos - objectPos2D).normalized;
        Vector2 directionToMouse = pos;
        RaycastHit2D hit = Physics2D.Raycast(objectPos2D, directionToMouse, _range, LayerMask.GetMask("MiningHit"));
        //Debug.Log("objectPos2D" + objectPos2D);
        if (hit.collider != null) {
            CreateLaserEffect(transform.InverseTransformPoint(objectPos2D), transform.InverseTransformPoint(hit.point));
            _hitParticleSystem.transform.position = hit.point;
            if (!_hitParticleSystem.isPlaying)
                _hitParticleSystem.Play();
        } else {
            // not in reange
            if (_hitParticleSystem.isPlaying)
                _hitParticleSystem.Stop();
            CreateLaserEffect(transform.InverseTransformPoint(objectPos2D), transform.InverseTransformPoint(objectPos2D + directionToMouse * _range));
        }
    }


    void CreateLaserEffect(Vector3 start, Vector3 end) {
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);

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
    }

    public void HandleVisualUpdateRemote(Vector2 nextInput) {
        _inputCurrent = nextInput;
        if (_inputCurrent != _inputPrev) {
            if (_currentRoutine != null) {
                StopCoroutine(_currentRoutine);
            }
            _currentRoutine = StartCoroutine(SmoothInterpolate(_inputPrev, _inputCurrent));
        }
    }
    private IEnumerator SmoothInterpolate(Vector2 from, Vector2 to) {
        float duration = 0.4f; // This should match the syncvar update frequency
        float elapsed = 0f;

        while (elapsed < duration) {
            elapsed += Time.deltaTime;
            Vector2 lerped = Vector2.Lerp(from, to, elapsed / duration);
            _inputPrev = lerped; // This makes sense right?
            LaserVisual(lerped);
            yield return null;
        }
        LaserVisual(to);
        _currentRoutine = null;// Cleanup
    }
}