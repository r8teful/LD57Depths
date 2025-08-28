using UnityEngine;
public class SubExterior : MonoBehaviour {
    [SerializeField] private OxygenZoneTrigger oxygenZoneCollider; 
    private void Start() {
         oxygenZoneCollider.SetEnabled(false); 
    }
}