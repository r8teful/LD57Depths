using UnityEngine;

public class OxygenZoneTrigger : MonoBehaviour {
    public void SetEnabled(bool b) {
        if(b) {
            GetComponent<CircleCollider2D>().enabled = true;
        } else {
            GetComponent<CircleCollider2D>().enabled = false;
        }
    }
    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            PlayerMovement playerMovement = collision.GetComponent<PlayerMovement>();
            if (playerMovement != null) {
                playerMovement.SetOxygenZone(true);
            }
        }
    }
    private void OnTriggerExit2D(Collider2D collision) {
        if (collision.CompareTag("Player")) {
            PlayerMovement playerMovement = collision.GetComponent<PlayerMovement>();
            if (playerMovement != null) {
                playerMovement.SetOxygenZone(false);
            }
        }
    }
}