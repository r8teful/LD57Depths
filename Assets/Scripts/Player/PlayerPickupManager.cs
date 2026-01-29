using System.Collections.Generic;
using UnityEngine;
using System.Collections.Generic; // top of file

public class PlayerPickupManager : MonoBehaviour, IPlayerModule {

    private float _pickupTimer;
    private float pickupRadius = 1f;
    [SerializeField] private LayerMask pickupLayerMask; // Set this to the layer your WorldItem prefabs are on
    private PlayerManager _player;
    private float _cachedMagnetism;
    private float MagnetRange => _cachedMagnetism * 2; // idk?
    private float MagnetStrength => _cachedMagnetism * 0.2f; // idk?
 
    public int InitializationOrder => 42;// Again no clue if this matters

    private HashSet<DropPooled> _currentlyMagnetized = new HashSet<DropPooled>(); // We keep that so we can call StopMagn

    public void InitializeOnOwner(PlayerManager playerParent) {
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
        // If nothing currently in range, stop magnetizing any previously magnetized items.
        if (results == null || results.Length == 0) {
            if (_currentlyMagnetized.Count > 0) {
                foreach (var prev in _currentlyMagnetized) {
                    if (prev != null) prev.OnStopMagnetizing();
                }
                _currentlyMagnetized.Clear();
            }
            return;
        }
        Vector2 center = transform.position;
        // Build a set of items that are currently in range & valid this frame.
        var current = new HashSet<DropPooled>();

        for (int i = 0; i < results.Length; i++) {
            var col = results[i];
            if (col == null) continue;
            var item = col.GetComponent<DropPooled>();
            if (item == null) continue;

            if (item.IsPicked) {
                continue;
            }

            current.Add(item);
            Vector2 toCenter = (center - (Vector2)item.transform.position);
            item.OnStartMagnetizing(toCenter, MagnetStrength);
        }

        // Any previously magnetized item that is not in the current set (or null) has been "lost"
        if (_currentlyMagnetized.Count > 0) {
            foreach (var prev in _currentlyMagnetized) {
                if (prev == null) continue;
                if (!current.Contains(prev) || prev.IsPicked) {
                    prev.OnStopMagnetizing();
                }
            }
        }

        // Replace the tracked set with the current set
        _currentlyMagnetized = current;
    }

    // Client performs a local check for nearby items
    private void PickupCheck() {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, pickupRadius, pickupLayerMask);
        // If nothing in range, allow next pickup request
        if (hits.Length <= 0) {
            return;
        }
        foreach (Collider2D hit in hits) {
            var item = hit.GetComponent<DropPooled>();
            if (item != null) {
                PickupItem(item);
            }
        }
    }

    private void PickupItem(DropPooled item) {
        ushort itemID = item.ItemID;
        AudioController.Instance.PlaySound2D("popPickup", 0.1f, pitch: new AudioParams.Pitch(AudioParams.Pitch.Variation.Small));
        _player.InventoryN.AwardItem(itemID,item.Amount);
        WorldDropManager.Instance.ReturnToPool(item);
    }
}