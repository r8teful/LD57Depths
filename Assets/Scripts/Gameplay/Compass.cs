using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class Compass : MonoBehaviour {
    [Header("References")]
    private Transform _playerTrans;
    [SerializeField] private Image _compasImage;
    [SerializeField] private CompassArrow arrowHandPrefab;
    private Coroutine ClosestEntityCoroutine;

    // Keyed by target transform for easy lookup and removal
    private CompassArrow _arrowNormal;
    private CompassArrow _arrowPlus;

    private void Awake() {
        _compasImage.gameObject.SetActive(false);
        PlayerRewardManager.OnRewardExecuted += OnRewardTaken;
    }

    private void OnRewardTaken(IExecutable obj) {
        // Simply star the coroutine over again
        if (ClosestEntityCoroutine != null) {
            StopCoroutine(ClosestEntityCoroutine);
            ClosestEntityCoroutine = StartCoroutine(FindClosestEntityRoutine());
        }
    }
    public void ActivateCompassPlus() {
        var closest = FindClosestEntity();
        if (closest == null) Debug.LogError("Coudn't find closest!");
        _arrowPlus = Instantiate(arrowHandPrefab, transform);
        _arrowPlus.Init(_playerTrans, closest,isPlus: true);
        ClosestEntityCoroutine = StartCoroutine(FindClosestEntityRoutine());
    }
    private IEnumerator FindClosestEntityRoutine() {
        while (true) {
            var closest = FindClosestEntity();
            if (closest != null) {
                _arrowPlus.UpdateTarget((Vector2)closest);
            }
            yield return new WaitForSeconds(1f);
        }
    }
    private Vector2? FindClosestEntity() {
        if (EntityManager.Instance == null) return null;
        var closest = EntityManager.Instance.FindClosestExplorationEntity(_playerTrans);
        if (closest != null) {
            return (Vector2)(Vector3)closest.cellPos;
        }
        return null;
    }
    public void ActivateNormal(PlayerManager player) {
        _playerTrans = player.transform;
        _compasImage.gameObject.SetActive(true);
        if (SubmarineManager.Instance != null) {
            _arrowNormal = Instantiate(arrowHandPrefab, transform);
            _arrowNormal.Init(_playerTrans, SubmarineManager.Instance.submarineExterior.transform);
        }
    }
    private void LateUpdate() {
        if (_arrowNormal !=null) {
            _arrowNormal.UpdateArrow();
        }
        if (_arrowPlus != null) {
            _arrowPlus.UpdateArrow();
        }
    }

}