using System.Collections.Generic;
using UnityEngine;
public class IceBounce : MonoBehaviour {

    public Rigidbody2D rb; // assign in inspector (will fallback to GetComponent in Awake)

    [Header("Detection")]
    [Tooltip("How many previous FixedUpdate frames to look back when deciding pre-impact speed.")]
    public int lookBackFrames = 3;
    [Tooltip("Minimum previous speed magnitude required to damage the tile")]
    public float minSignificantSpeed = 1.5f;
    [Tooltip("How small the current speed must be to count as stopped")]
    public float stopThreshold = 0.05f; // near zero
    [Tooltip("Minimum opposing speed required to count as a bounce")]
    public float bounceMinOpposingSpeed = 0.5f;
    [Tooltip("Minimum drop required between previous max and current to consider it a big change.")]
    public float minDelta = 1.0f;
    [Tooltip("Ignore repeated triggers for this many fixed-updates after a detection")]
    public int cooldownFrames = 3;

    // internal
    Queue<Vector2> _history;
    int _cooldownCounter;
    private PlayerManager _player;

    public void Init(AbilityInstance instance, PlayerManager player) {
        Debug.Log("Ice bounce equiped!");
        _history = new Queue<Vector2>(lookBackFrames + 1);
        _cooldownCounter = 0;
        _player = player;
    }
    private void Start() {
        // Playermovement is not initialized in our init functoin, so wait until start to get the reference
        if (rb == null) rb = PlayerManager.LocalInstance.PlayerMovement.GetRigidbody();
    }
    private void OnDestroy() {
        Debug.Log("Ice bounce GONE!");
    }
    void FixedUpdate() {
        if (rb == null) return;

        Vector2 current = rb.linearVelocity;
        //Debug.Log(current);
        // Check only if we have any history to compare against
        if (_history.Count > 0 && _cooldownCounter <= 0) {
            // Find maximum absolute previous value for each axis and keep the signed val at that moment
            float maxPrevXAbs = 0f; float signedAtMaxX = 0f;
            float maxPrevYAbs = 0f; float signedAtMaxY = 0f;

            foreach (var v in _history) {
                float ax = Mathf.Abs(v.x);
                if (ax > maxPrevXAbs) { maxPrevXAbs = ax; signedAtMaxX = v.x; }
                float ay = Mathf.Abs(v.y);
                if (ay > maxPrevYAbs) { maxPrevYAbs = ay; signedAtMaxY = v.y; }
            }

            float currentXAbs = Mathf.Abs(current.x);
            float currentYAbs = Mathf.Abs(current.y);

            if (maxPrevXAbs >= minSignificantSpeed) {
                bool stopped =
                    currentXAbs <= stopThreshold &&
                    (maxPrevXAbs - currentXAbs) >= minDelta;

                bool bounced =
                    Mathf.Sign(signedAtMaxX) != Mathf.Sign(current.x) &&
                    Mathf.Abs(current.x) >= bounceMinOpposingSpeed;

                if (stopped || bounced) {
                    TriggerImpact("X", maxPrevXAbs, signedAtMaxX, current);
                }
            }

            // Y axis
            if (maxPrevYAbs >= minSignificantSpeed) {
                bool stopped =
                    currentYAbs <= stopThreshold &&
                    (maxPrevYAbs - currentYAbs) >= minDelta;

                bool bounced =
                    Mathf.Sign(signedAtMaxY) != Mathf.Sign(current.y) &&
                    Mathf.Abs(current.y) >= bounceMinOpposingSpeed;

                if (stopped || bounced) {
                    TriggerImpact("Y", maxPrevYAbs, signedAtMaxY, current);
                }
            }
        }

        // push current into history, limit length to lookBackFrames
        _history.Enqueue(current);
        while (_history.Count > lookBackFrames) _history.Dequeue();

        if (_cooldownCounter > 0) _cooldownCounter--;
    }

    private void TriggerImpact(string axis, float preImpactMagnitudeAbs, float signedValueAtMax, Vector2 currentVelocity) {
        //Debug.Log($"Hard impact on axis {axis}: pre-impact speed = {preImpactMagnitudeAbs:F2} (signed {signedValueAtMax:F2}) -> current {(axis == "X" ? currentVelocity.x : currentVelocity.y):F2}");
        var dmg = GetContactDamage(preImpactMagnitudeAbs);
        var contacts = _player.PlayerMovement.ContactsMostRecent;
        foreach (var contact in contacts) {
            _player.RequestDamageTile(contact.point, dmg);
        }
        // start cooldown to avoid repeated triggers for the same collision
        _cooldownCounter = cooldownFrames;
    }

    private float GetContactDamage(float velocity) {
        return velocity;
    }
}