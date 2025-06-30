using DG.Tweening;
using System.Collections;
using UnityEngine;

public class MiningLazer : MonoBehaviour, IToolBehaviour {
    [Header("Raycast Gun Settings")]
    public float innerSpotAngle = 5f;
    public float outerSpotAngle = 30f;
    public float range = 10f;
    public float falloffStrength = 1.5f; // Higher values = faster falloff
    public float frequency = 10f;        // Rays per second
    public float damagePerRay = 10f;     // Base damage per ray
    public bool CanMine { get; set; } = true;
    private bool _isMining;
    private InputManager _inputManager;
    private Coroutine miningRoutine;
    private float timer = 0f;
    private AudioSource laser;
    public LineRenderer lineRenderer; 
    public ParticleSystem ParticlesPrefabHit;
    public ParticleSystem _lineLazerParticleSystem;
    private ParticleSystem _hitParticleSystem;

    private void Awake() {
        UpgradeManager.UpgradeBought += OnUpgraded;
    }
    private void OnDestroy() {
        UpgradeManager.UpgradeBought -= OnUpgraded;
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
    public void ToolStart(InputManager input, ToolController controller) {
        if (!CanMine) return;
        if(miningRoutine != null) {
            Debug.LogWarning("Mining routine is still running even though it should have stopped!");
            StopCoroutine(miningRoutine);
        }
        _inputManager = input;
        _isMining = true;
        FadeInLine(lineRenderer);
        miningRoutine = StartCoroutine(MiningRoutine(controller));
    }
    public void ToolStop() {
        if(miningRoutine != null) {
            StopCoroutine(miningRoutine);
            miningRoutine = null;
            _hitParticleSystem.Stop();
            laser.volume = 0.0f;
            _isMining = false;
            FadeOutLine(lineRenderer);
            if (_lineLazerParticleSystem.isPlaying)
                _lineLazerParticleSystem.Stop();
        }
    }

    private void FadeOutLine(LineRenderer lineRenderer) {
        Color2 startColor = new(lineRenderer.startColor,lineRenderer.endColor);
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

    private IEnumerator MiningRoutine(ToolController controller) {
        while (true) {
            laser.volume = 0.2f;
            var pos = _inputManager.GetAimInput();
            //Debug.Log(pos);
            var isFlipped = false;
            var horizontalInput = _inputManager.GetMovementInput().x;
           
            CastRays(pos, controller, isFlipped); // Todo determine freq here
            //LaserVisual(pos);
            yield return new WaitForSeconds(0.3f);
            laser.volume = 0f;
        }
    }
    private void Update() {
        if (_isMining) {
            var pos = _inputManager.GetAimInput();
            SetCorrectLaserPos(_inputManager.GetMovementInput().x);
            LaserVisual(pos);
        }
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
        Vector2 directionToMouse = (pos - objectPos2D).normalized;
        RaycastHit2D hit = Physics2D.Raycast(objectPos2D, directionToMouse, range, LayerMask.GetMask("MiningHit"));
        //Debug.Log("objectPos2D" + objectPos2D);
        if(hit.collider != null) {
            CreateLaserEffect(transform.InverseTransformPoint(objectPos2D), transform.InverseTransformPoint(hit.point));
            _hitParticleSystem.transform.position = hit.point;
            if (!_hitParticleSystem.isPlaying)
                _hitParticleSystem.Play();
        } else {
            // not in reange
            if (_hitParticleSystem.isPlaying)
                _hitParticleSystem.Stop();
            CreateLaserEffect(transform.InverseTransformPoint(objectPos2D), transform.InverseTransformPoint(objectPos2D + directionToMouse * range));
        }
    }

    void CastRays(Vector2 pos, ToolController controller, bool isFlipped) {
        Vector2 objectPos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 directionToMouse = (pos - objectPos2D).normalized;
        //Vector2 rayDirection = GetConeRayDirection(directionToMouse);
        Vector2 rayDirection = directionToMouse;
        RaycastHit2D hit = Physics2D.Raycast(transform.position, rayDirection, range, LayerMask.GetMask("MiningHit"));
        if (hit.collider != null) {
            // Just assuming here that we've hit a tile, but should be fine because of the mask
            Vector2 nudgedPoint = hit.point - rayDirection * -0.1f;
            //float distance = hit.distance;
            //float falloffFactor = Mathf.Clamp01(1f - (distance / range) * falloffStrength);
            //float finalDamage = damagePerRay * falloffFactor;
            controller.CmdRequestDamageTile(new Vector3(nudgedPoint.x, nudgedPoint.y,0), (short)damagePerRay);
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
        _lineLazerParticleSystem.transform.rotation = Quaternion.Euler(0, 0, angle );
        // Configure the particle system's shape
        var shape = _lineLazerParticleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.SingleSidedEdge;
        shape.radius = distance / 2;
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