using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Handles a visual of one item going from player to the upgrademanager
public class SubItemTransferVisual : MonoBehaviour {
    private Coroutine _moveRoutine;
    [SerializeField] private SpriteRenderer _sprite;
    public void StartVisual(Transform start, Transform dest,Sprite image, int quantity) {
        _moveRoutine ??= StartCoroutine(StartVisualRoutine(start,dest, image, quantity));
    }

    public void StopVisual() {
        if (_moveRoutine == null) return;
        StopCoroutine(_moveRoutine);
        _moveRoutine = null;
        Destroy(gameObject);
    
    }

    private IEnumerator StartVisualRoutine(Transform start, Transform dest, Sprite image, int quantity) {
        int visualCount = Mathf.Clamp(quantity, 3, 20);

        float travelTime = 0.5f;
        // Calculate delay so they are evenly spaced out based on travel time
        float interval = travelTime / visualCount;

        for (int i = 0; i < visualCount; i++) {
            var visual = Instantiate(_sprite,transform);
            visual.transform.position = start.position;
            visual.sprite = image;
            MoveSpriteCycle(visual.gameObject, start, dest, travelTime);
            yield return new WaitForSeconds(interval);
        }
    }
    private void MoveSpriteCycle(GameObject obj, Transform start, Transform dest, float duration) {
        if (obj == null || start == null || dest == null) return;

        obj.transform.position = start.position;

        obj.transform.DOMove(dest.position, duration)
            .SetEase(Ease.Linear) 
            .OnComplete(() => {
                MoveSpriteCycle(obj, start, dest, duration);
            });
    }
}