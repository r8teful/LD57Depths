using UnityEngine;
using System.Collections;

public class UpgradeCollider : MonoBehaviour {

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.gameObject.CompareTag("Player")) {
            SubInside.Instance.EnterUpgrade();
        }
    }
    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.gameObject.CompareTag("Player")) {
            SubInside.Instance.ExitCollider();
        }
    }
}