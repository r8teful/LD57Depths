using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

public enum FishBackgroundState {
    Chilling,
    TooFar
}
public class FishBackgroundVisual : MonoBehaviour, IBackgroundObject {
    [SerializeField] float _maxDistFromWorldCentre = 20;
    public float thrustInterval;
    public float turnTorque;
    public float maxSpeed;
    public float fishPower;
    [ShowInInspector]
    private FishBackgroundState _currentState;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Vector3 _worldCentrePos;
    private float thrustTimer;
    private float impulseMagnitude;
    private Sequence _currentSeq;

    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        // Start as transparent
        var color = spriteRenderer.color;
        color.a = 0;
        spriteRenderer.color = color;
    }
    public void Init(Color backgroundColor, int l, int orderInLayer) {
        spriteRenderer.color = backgroundColor;
        spriteRenderer.sortingOrder = orderInLayer;
        float s = Mathf.Max(0.01f,0.4f - 0.09f * l);
        var scale = new Vector3(s,s,s);
        transform.localScale = scale;
        transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
        spriteRenderer.DOFade(1, 3);
    }
    private void FixedUpdate() {
        // AI logic should only run on the server. FishNet handles synchronization.
        UpdateState();
        PerformStateAction();
    }
    private void UpdateState() {
        _worldCentrePos = new(0, transform.position.y);
        float distanceToWorldCentre = Vector2.Distance(transform.position, _worldCentrePos);

        if (distanceToWorldCentre > _maxDistFromWorldCentre) {
            _currentState = FishBackgroundState.TooFar;
        } else {
            _currentState = FishBackgroundState.Chilling;
        }
    }
    private void PerformStateAction() {
        // 3. Move the fish using torque and force for smooth, physical movement
        MoveWithPhysics(CalculatePrimaryTargetDirection(), maxSpeed);

        // 4. Flip the sprite
        FlipSprite();
    }

    private Vector2 CalculatePrimaryTargetDirection() {

        switch (_currentState) {
            case FishBackgroundState.Chilling:
                if (rb.linearVelocity.magnitude < 0.1f && Random.Range(0, 200) < 1) {
                    //Debug.Log("random target");
                    return Random.insideUnitCircle.normalized;
                }
                return rb.linearVelocity.normalized; // Continue in the same direction
            case FishBackgroundState.TooFar:
                return (_worldCentrePos - transform.position).normalized;
            default:
                return transform.up;
        }
    }
    private void MoveWithPhysics(Vector2 moveDirection, float maxSpeed) {
        // --- ROTATION ---
        float angleDifference = Vector2.SignedAngle(transform.right, moveDirection);
        float rotationAmount = angleDifference * (turnTorque / 100f) * Time.fixedDeltaTime;
        rb.AddTorque(rotationAmount);
        // Dampen angular velocity to prevent overshooting
        rb.angularVelocity *= 0.9f;

        // --- THRUST ---
        float averageForce = rb.mass * fishPower;
        impulseMagnitude = averageForce;// * thrustInterval; // Don't really want to scale it because it will just result in the same quanitity of movement

        thrustTimer -= Time.fixedDeltaTime;
        if (thrustTimer <= 0f) {
            Vector2 impulse = (Vector2)transform.right * impulseMagnitude;
            rb.AddForce(impulse, ForceMode2D.Impulse);
            _currentSeq = DoFishWiggle();
            thrustTimer += thrustInterval;
        }

        // --- SPEED CONTROL ---
        if (rb.linearVelocity.magnitude > maxSpeed) {
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed;
        }
    }
    private Sequence DoFishWiggle() {
        if (transform == null)
            return null;
        var xScale = transform.localScale.x;
        var yScale = transform.localScale.y;
        Sequence moveSeq = DOTween.Sequence();
        moveSeq.SetLink(gameObject);
        moveSeq.Append(transform.DOScaleX(xScale * 0.8f, 0.2f));
        moveSeq.Insert(0.05f, transform.DOScaleY(yScale * 1.2f, 0.2f));
        moveSeq.Append(transform.DOScaleX(xScale, 0.2f).SetEase(Ease.OutBounce));
        moveSeq.Join(transform.DOScaleY(yScale, 0.2f).SetEase(Ease.OutBounce));
        return moveSeq;
    }
    private void FlipSprite() {
        if (spriteRenderer == null)
            return;
        float zRotation = transform.localEulerAngles.z;

        // Normalize rotation to [0, 360)
        zRotation = zRotation % 360;

        if (zRotation > 90f && zRotation < 270f) {
            spriteRenderer.flipY = true;
        } else {
            spriteRenderer.flipY = false;
        }
    }

    public void BeforeDestroy() {
        spriteRenderer.DOFade(0, 3).OnComplete(() => DestroyObject());
    }

    private void DestroyObject() {
        _currentSeq.Kill();
        transform.DOKill();
        Destroy(gameObject, 0.1f);
    }
}