using UnityEngine;
public class SubExterior : MonoBehaviour {
    [SerializeField] private OxygenZoneTrigger oxygenZoneCollider; 
    private void Start() {
         oxygenZoneCollider.SetEnabled(false); 
    }
    private void OnEnable() {
        // Subscribe to the event to recalculate stats when a NEW upgrade is bought
        UpgradeManagerPlayer.OnUpgradePurchased += HandleUpgradePurchased;
    }
    private void OnDisable() {
        UpgradeManagerPlayer.OnUpgradePurchased -= HandleUpgradePurchased;
    }


    private void HandleUpgradePurchased(UpgradeRecipeBase data) {
        if(data.type == UpgradeType.OxygenZoneUnlock) {
            Debug.Log($"Oxygen Zone Upgrade purchased: {data.name}");
            oxygenZoneCollider.SetEnabled(true);
            // todo visuals
        }
    }
}