using UnityEngine;
using System.Collections;

public class ShipCollider : MonoBehaviour {

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.gameObject.CompareTag("Player")) {
            SubInside.Instance.EnterShipControll();
        }
    }
    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.gameObject.CompareTag("Player")) {
            SubInside.Instance.ExitCollider();
        }
    }
}