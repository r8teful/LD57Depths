using UnityEngine;
using System.Collections;
public class ExitCollider : MonoBehaviour {
    private void OnTriggerEnter2D(Collider2D collision) {
        Debug.Log("ENTER!");        
        if (collision.gameObject.CompareTag("Player")) {
            SubInside.Instance.EnterExit();
        }
    }
    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.gameObject.CompareTag("Player")) {
            SubInside.Instance.ExitCollider();
        }
    }
}