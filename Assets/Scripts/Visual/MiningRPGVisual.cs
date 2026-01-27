using System.Collections;
using UnityEngine;

// Mostly a copy of what I made in MiningDrillVisual
public class MiningRPGVisual : MonoBehaviour, IToolVisual {
    [SerializeField] private SpriteRenderer _spriteRPG;
    [SerializeField] private SpriteRenderer _spriteHand;
    [SerializeField] private Sprite _spriteSwiming;
    [SerializeField] private Sprite _spriteStanding;
    private Vector2 _inputPrev;
    private Vector2 _inputCurrent;
    private Coroutine _currentRoutine;
    private bool _isOwner;
    private PlayerVisualHandler _cachedVisualHandler;
    private Vector2 _nextInput;

    public (Sprite, Sprite) BackSprites => (_spriteSwiming,_spriteStanding);

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
            RPGVisual(lerped);
            yield return null;
        }
        RPGVisual(to);
        _currentRoutine = null;// Cleanup
    }

    private void RPGVisual(Vector2 pos) {
        Vector2 objectPos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 directionToMouse = (pos - objectPos2D).normalized;
        float angle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void Init(bool isOwner, PlayerManager networkedPlayer) {
        _isOwner = isOwner;
        // Cache visual handler
        _cachedVisualHandler = networkedPlayer.PlayerVisuals;
        // Init is NOT when we equip, its called on Initialize, we should have a different way to handle when we actually equip a tool
        //visuaHandler.PlayerVisuals.OnToolInitBack(this as IToolVisual); // Tell the backvisual that we're the rpg is now equiped
    }

    // Called once every frame for owner, and once every 0.4s for non owners
    public void UpdateVisual(object data, InputManager inputManager = null) {
        if (_isOwner) { 
                Vector2 dir;
            if (data is Vector2 inputDir) {
                dir = inputDir;
            } else {
                dir = inputManager.GetDirFromPos(transform.position);
            }
            RPGVisual(dir);
        } else {
            // Non owner
            if (data is Vector2 vector) {
                // Update the target position. update will handle the smooth movement.
                _nextInput = vector;
                Debug.Log($"Setting next input to: {_nextInput}");
            } else {
                Debug.LogWarning($"Inputdata is not a vector2!");
            }
        }
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
    public void StartVisual() {
        if (_cachedVisualHandler != null)
            _cachedVisualHandler.OnStartDrilling();
        _spriteRPG.enabled = true;
        _spriteHand.enabled = true;
    }

    public void StopVisual() {
        if (_cachedVisualHandler != null)
            _cachedVisualHandler.OnStopDrilling();
        _spriteRPG.enabled = false;
        _spriteHand.enabled = false;
    }

    public void FlipVisual(bool isFlipped) {
        // Do nothing
    }

}