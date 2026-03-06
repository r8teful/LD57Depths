using UnityEngine;
using DG.Tweening;
using Sirenix.OdinInspector;
public class UpDownTween : MonoBehaviour {
    //[InfoBox("Distance in localspace")]
    [OnValueChanged("Recalculate")]
    public float Distance;
    //[InfoBox("Time in seconds")]
    [OnValueChanged("Recalculate")]
    public float Duration;

    void Start() {
        Recalculate();
    }

    void Recalculate() {
        transform.DOKill();
        transform.DOLocalMoveY(Distance, Duration).SetRelative().SetLoops(-1,LoopType.Yoyo).SetEase(Ease.InOutSine);
    }
}
