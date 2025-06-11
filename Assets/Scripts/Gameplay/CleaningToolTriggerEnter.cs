using UnityEngine;

public class CleaningToolTriggerEnter : MonoBehaviour {
    [SerializeField] private CleaningTool _parentCleaningTool;

    private void OnTriggerEnter2D(Collider2D collision) {
        if (collision.CompareTag("Waste")) {
            _parentCleaningTool.OnTriggerWasteEnter(collision.gameObject);
        }
    }
}
