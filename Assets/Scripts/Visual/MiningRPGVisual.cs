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

    public (Sprite, Sprite) BackSprites => (_spriteSwiming,_spriteStanding);

    public void HandleVisualStart(PlayerVisualHandler playerVisualHandler) {
        // Show the drill
        playerVisualHandler.OnStartDrilling();
        _spriteRPG.enabled = true;
        _spriteHand.enabled = true;
    }
    public void HandleVisualStop(PlayerVisualHandler playerVisualHandler) {
        // Hide the drill
        playerVisualHandler.OnStopDrilling();
        _spriteRPG.enabled = false;
        _spriteHand.enabled = false;
    }

    public void HandleVisualUpdate(Vector2 dir, InputManager inputManager, bool isAbility) {
        RPGVisual(inputManager.GetAimWorldInput());
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

    public void Init(bool isOwner, NetworkedPlayer visuaHandler) {
        visuaHandler.PlayerVisuals.OnToolInitBack(this as IToolVisual); // Tell the backvisual that we're the rpg is now equiped
    }

    public void UpdateVisual(object data, InputManager inputManager = null) {
        throw new System.NotImplementedException();
    }

    public void StartVisual() {
        throw new System.NotImplementedException();
    }

    public void StopVisual() {
        throw new System.NotImplementedException();
    }
}