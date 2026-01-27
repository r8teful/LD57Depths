using System.Collections;
using UnityEngine;

public class MiningDrillVisual : MonoBehaviour, IToolVisual {
    [SerializeField] private SpriteRenderer _spriteDrill;
    [SerializeField] private SpriteRenderer _spriteHand;
    private Vector2 _inputPrev;
    private Vector2 _inputCurrent;
    private Coroutine _currentRoutine;
    private PlayerVisualHandler _cachedVisualHandler;
    private AudioSource drill;
    private bool _isOwner;

    public (Sprite, Sprite) BackSprites => (null,null);

    public void HandleVisualStart(PlayerVisualHandler playerVisualHandler) {
        // Show the drill
        playerVisualHandler.OnStartDrilling();
    }
    public void HandleVisualStop(PlayerVisualHandler playerVisualHandler) {
        // Hide the drill
        playerVisualHandler.OnStopDrilling();
    }

    public void HandleVisualUpdate(Vector2 dir, InputManager inputManager,bool isAbility) {
        DrillVisual(inputManager.GetAimWorldInput());
    }

    // TODO Should happen within update by checking if we are owner or not
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
            DrillVisual(lerped);
            yield return null;
        }
        DrillVisual(to);
        _currentRoutine = null;// Cleanup
    }

    private void DrillVisual(Vector2 pos) {
        Vector2 objectPos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 directionToMouse = (pos - objectPos2D).normalized;
        float angle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void Init(bool isOwner, PlayerManager parent) {
        _isOwner = isOwner;
        _cachedVisualHandler = parent.PlayerVisuals;
        //parent.PlayerVisuals.OnToolInitBack(this); // Removes whatever backvisual we've got
        drill = AudioController.Instance.PlaySound2D("Drill", 0.0f, looping: true);
    }

    public void UpdateVisual(object data, InputManager inputManager = null) {
        if (_isOwner) {
            Vector2 dir;
            if (data is Vector2 inputDir) {
                dir = inputDir;
            } else {
                dir = inputManager.GetDirFromPos(transform.position);
            }
            DrillVisual(dir);
        } else {
            if (data is Vector2 vector) {
                // Update the target position. update will handle the smooth movement.
                // TODO handle remote input
                //_nextInput = vector;
                //Debug.Log($"Setting next input to: {_nextInput}");
            } else {
                Debug.LogWarning($"Inputdata is not a vector2!");
            }
        }
    }

    public void StartVisual() {
        if (_cachedVisualHandler != null)
            _cachedVisualHandler.OnStartDrilling();
        drill.volume = 0.2f;

        _spriteDrill.enabled = true;
        _spriteHand.enabled = true;
    }

    public void StopVisual() {
        if (_cachedVisualHandler != null)
            _cachedVisualHandler.OnStopDrilling();
        drill.volume = 0;
        _spriteDrill.enabled = false;
        _spriteHand.enabled = false;
    }

    public void FlipVisual(bool isFlipped) {
        // Do nothing
    }
}