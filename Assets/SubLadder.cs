using DG.Tweening;
using UnityEngine;

public class SubLadder : MonoBehaviour {
    public Transform LadderStartPos;
    public Transform LadderEndPos;
    public Transform Ladder;
    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            //Lower the ladder
            Ladder.DOMove(LadderEndPos.position, 0.4f);
        }
    }
    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            Ladder.DOKill();
            Ladder.DOMove(LadderStartPos.position, 0.4f);
        }
    }
}
