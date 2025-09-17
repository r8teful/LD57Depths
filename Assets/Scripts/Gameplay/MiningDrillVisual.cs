using System.Collections;
using UnityEngine;

public class MiningDrillVisual : MonoBehaviour, IToolVisual {
    [SerializeField] private SpriteRenderer _spriteDrill;
    [SerializeField] private SpriteRenderer _spriteHand;
    private Vector2 _inputPrev;
    private Vector2 _inputCurrent;
    private Coroutine _currentRoutine;
    public void HandleVisualStart(PlayerVisualHandler playerVisualHandler) {
        // Show the drill
        playerVisualHandler.OnStartDrilling();
        _spriteDrill.enabled = true;
        _spriteHand.enabled = true;
    }
    public void HandleVisualStop(PlayerVisualHandler playerVisualHandler) {
        // Hide the drill
        playerVisualHandler.OnStopDrilling();
        _spriteDrill.enabled = false;
        _spriteHand.enabled = false;
    }

    public void HandleVisualUpdate(Vector2 dir, InputManager inputManager,bool isAbility) {
        DrillVisual(inputManager.GetAimWorldInput());
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
            DrillVisual(lerped);
            yield return null;
        }
        DrillVisual(to);
        _currentRoutine = null;// Cleanup
    }
    public void Init(IToolBehaviour parent) {
        // Don't need any special visuals atm
    }

    private void DrillVisual(Vector2 pos) {
        Vector2 objectPos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 directionToMouse = (pos - objectPos2D).normalized;
        float angle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}