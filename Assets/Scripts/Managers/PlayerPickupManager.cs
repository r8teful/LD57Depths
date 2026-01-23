using UnityEngine;

public class PlayerPickupManager : MonoBehaviour, INetworkedPlayerModule {

    private float _pickupTimer;
    private float pickupRadius = 0.5f;
    private float magnetRadius = 3f;
    [SerializeField] private LayerMask pickupLayerMask; // Set this to the layer your WorldItem prefabs are on
    private NetworkedPlayer _player;
    private float _cachedMagnetism;
    private float MagnetRange => _cachedMagnetism * 2; // idk?
    private float MagnetStrength => _cachedMagnetism * 0.2f; // idk?
 
    public int InitializationOrder => 42;// Again no clue if this matters
    public void InitializeOnOwner(NetworkedPlayer playerParent) {
        _player = playerParent;
        _player.PlayerStats.OnStatChanged += OnStatChange;
        _cachedMagnetism = _player.PlayerStats.GetStat(StatType.PlayerMagnetism);
    }

    private void OnStatChange() {
        // Cache new magnetism value
        _cachedMagnetism = _player.PlayerStats.GetStat(StatType.PlayerMagnetism);
    }

    private void Update() {
        _pickupTimer -= Time.deltaTime;
        if (_pickupTimer <= 0f) {
            PickupCheck();
            _pickupTimer = 0.1f; // pickup every 0.1s
        }
    }
    private void FixedUpdate() {
        MagnetCheck();
    }

    private void MagnetCheck() {
        var results = Physics2D.OverlapCircleAll(transform.position, MagnetRange, pickupLayerMask);
        if (results.Length <= 0) return;

        Vector2 center = transform.position;
        for (int i = 0; i < results.Length; i++) {
            var col = results[i];
            if (col == null) continue;
            var item = col.GetComponent<DroppedEntity>();
            if (item == null || item.IsPicked) continue;
            Vector2 toCenter = (center - (Vector2)item.transform.position);
            item.OnStartMagnetizing(toCenter, MagnetStrength);
        }
    }

    // Client performs a local check for nearby items
    private void PickupCheck() {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius, pickupLayerMask);
        // If nothing in range, allow next pickup request
        if (hits.Length <= 0) {
            return;
        }
        foreach (Collider2D hit in hits) {
            var item = hit.GetComponent<DroppedEntity>();
            if (item != null) {
                PickupItem(item);
            }
        }
    }

    private void PickupItem(DroppedEntity item) {
        ushort itemID = item.ItemID;
        AudioController.Instance.PlaySound2D("popPickup", 0.1f, pitch: new AudioParams.Pitch(AudioParams.Pitch.Variation.Small));
        _player.InventoryN.AwardItem(itemID);
        Destroy(item.gameObject);
    }
}