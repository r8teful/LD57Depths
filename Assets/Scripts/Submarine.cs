using UnityEngine;
using System.Collections;
using DG.Tweening;


public class Submarine : StaticInstance<Submarine> {
    public Transform insideSubmarinePosition;  // Where the player appears inside

    private Vector3 outsideSubmarinePosition;
    private bool insideSubmarine = false;
    private PlayerController player;
    private bool _isCutscene;
    void Start() {
        player = PlayerController.Instance; // Find the player
    }
    public void setOutideSubPos(Vector3 pos) {
        outsideSubmarinePosition = pos;
    }
    void OnTriggerEnter2D(Collider2D other) {
        if (_isCutscene) return;
        if (other.CompareTag("Player")) {
            if (insideSubmarine) {
                ExitSub();
            } else {
                // Store the current outside position so we can return to it later.
                //outsideSubmarinePosition = player.transform.position;
                var p = new Vector3(transform.position.x,transform.position.y-0.3f,transform.position.z);
                outsideSubmarinePosition = p;

                //Debug.Log("Enter! pos:" + outsideSubmarinePosition);
                //var p = outsideSubmarinePosition.position;
                //outsideSubmarinePosition.position = new Vector3(p.x, p.y - 0.5f, p.z);
                EnterSub();
            }
        }
    }

    public void EnterSub() {
        // Position the player inside the submarine and update their state.
        insideSubmarine = true;
        player.transform.position = insideSubmarinePosition.Find("PlayerSpawn").position;
        player.GetComponent<PlayerController>().SetState(PlayerController.PlayerState.Ship);
        SubInside.Instance.PlayerEntered();
    }

    public void ExitSub() {
        insideSubmarine = false;
        // When exiting, you may want the player to start at the turning point.
        // For example, if you have an "outsideTurning" position in the PlayerController, you could snap them there.
        // player.transform.position = player.GetComponent<PlayerController>().outsideTurning;
        // Otherwise, we use the stored outside position.
        //Debug.Log("Exit! pos:" + outsideSubmarinePosition);
        player.transform.position = outsideSubmarinePosition;
        player.GetComponent<PlayerController>().SetState(PlayerController.PlayerState.Swimming);
    }
    public IEnumerator Cutscene(float distanceRatio, int length) {
        _isCutscene = true;
        // Dissable interactions, close menu etc
        ShipManager.Instance.ShipClose();
        ExitSub(); // Sounds counter intuative but we want to see the outside 
        var d = transform.position.y + GridManager.Instance.GetWorldHeightFromRatio(distanceRatio);
        transform.DOMoveY(d, length);
        player.CutsceneStart();
        yield return new WaitForSeconds(length);
        SubInside.Instance.IncreaseBackgroundLevel();
        var p = new Vector3(transform.position.x, transform.position.y - 0.3f, transform.position.z);
        outsideSubmarinePosition = p;
        player.CutsceneEnd();
        _isCutscene = false ;
    }
}
