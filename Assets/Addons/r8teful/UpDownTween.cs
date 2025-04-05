using UnityEngine;
using DG.Tweening;
//using Sirenix.OdinInspector;
using static UnityEngine.RuleTile.TilingRuleOutput;
public class UpDownTween : MonoBehaviour {
    //[InfoBox("Distance in localspace")]
   // [OnValueChanged("Recalculate")]
    public float Distance;
    //[InfoBox("Time in seconds")]
    //[OnValueChanged("Recalculate")]
    public float Speed;

    private Vector3 _startPos;
    void Start() {
        _startPos = transform.localPosition;
        Recalculate();
    }

    void Recalculate() {
        var p = _startPos;
        p.y -= Distance * 0.5f;
        transform.localPosition = p;
        transform.DOKill();
        transform.DOLocalMoveY(Distance, Speed).SetLoops(-1,LoopType.Yoyo).SetEase(Ease.InOutSine);
    }
}
