using UnityEngine;
using System.Collections;


public class Submarine : StaticInstance<Submarine> {
    public Transform insideSubmarinePosition;  // Where the player appears inside

    private Vector3 outsideSubmarinePosition;
    private bool insideSubmarine = false;
    private GameObject player;

    void Start() {
        player = GameObject.FindGameObjectWithTag("Player"); // Find the player
    }

    void OnTriggerEnter2D(Collider2D other) {
        if (other.CompareTag("Player")) {
            if (insideSubmarine) {
                ExitSub();
            } else {
                // Store the current outside position so we can return to it later.
                outsideSubmarinePosition = player.transform.position;

                //Debug.Log("Enter! pos:" + outsideSubmarinePosition);
                //var p = outsideSubmarinePosition.position;
                //outsideSubmarinePosition.position = new Vector3(p.x, p.y - 0.5f, p.z);
                EnterSub();
            }
            insideSubmarine = !insideSubmarine;
        }
    }

    public void EnterSub() {
        // Position the player inside the submarine and update their state.
        player.transform.position = insideSubmarinePosition.position;
        player.GetComponent<PlayerController>().CurrentState = PlayerController.PlayerState.Outside;
    }

    public void ExitSub() {
        // When exiting, you may want the player to start at the turning point.
        // For example, if you have an "outsideTurning" position in the PlayerController, you could snap them there.
        // player.transform.position = player.GetComponent<PlayerController>().outsideTurning;
        // Otherwise, we use the stored outside position.
        //Debug.Log("Exit! pos:" + outsideSubmarinePosition);
        player.transform.position = outsideSubmarinePosition;
        player.GetComponent<PlayerController>().CurrentState = PlayerController.PlayerState.Swimming;
    }
}
