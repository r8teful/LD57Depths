using System.Collections.Generic;
using UnityEngine;

public class PlayerPickupManager : MonoBehaviour, IPlayerModule, IValueModifiable {

    private float _pickupTimer;
    private float pickupRadius = 1f;
    [SerializeField] private LayerMask pickupLayerMask; // Set this to the layer your WorldItem prefabs are on
    private PlayerManager _player;
    private float _magnetStrengthBase = 0.2f;
    private float _magnetStrength;
    private float _magnetRangeBase = 2;
    private float _magnetRange;
    
    public int InitializationOrder => 42;// Again no clue if this matters

    private HashSet<DropPooled> _currentlyMagnetized = new HashSet<DropPooled>(); // We keep that so we can call StopMagn

    public void InitializeOnOwner(PlayerManager playerParent) {
        _player = playerParent;
        Register();
        _magnetStrength = _magnetStrengthBase;
        _magnetRange = _magnetRangeBase;
    }


    private void Update() {
        if (_player == null) return;
        if (!_player.PlayerMovement.CanPickup()) return;
        _pickupTimer -= Time.deltaTime;
        if (_pickupTimer <= 0f) {
            PickupCheck();
            _pickupTimer = 0.1f; // pickup every 0.1s
        }
    }
    private void FixedUpdate() {
        if (!_player.PlayerMovement.CanPickup()) return;
        MagnetCheck();
    }

    private void MagnetCheck() {
        var results = Physics2D.OverlapCircleAll(transform.position, _magnetRange, pickupLayerMask);
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
            item.OnStartMagnetizing(toCenter, _magnetStrength);
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
            if (item != null && !item.IsPicked) {
                //Debug.Log("pickup!");
                PickupItem(item);
            }
        }
    }

    private void PickupItem(DropPooled item) {
        item.IsPicked = true;
        ushort itemID = item.ItemID;
        AudioController.Instance.PlaySound2D("popPickup", 0.1f, pitch: new AudioParams.Pitch(AudioParams.Pitch.Variation.Small));
        _player.InventoryN.AwardItem(itemID,item.Amount);
        WorldTileManager.Instance.ReturnToPool(item);
    }

    public void ModifyValue(ValueModifier modifier) {
        if(modifier.Key == ValueKey.MagnetismPickup) {
            var newV = UpgradeCalculator.CalculateUpgradeChange(_magnetRange,modifier);
            _magnetRange = newV; 
        } else if(modifier.Key == ValueKey.MagnetismStrength) {
            var newV = UpgradeCalculator.CalculateUpgradeChange(_magnetStrength, modifier);
            _magnetStrength = newV; 
        }
    }

    public float GetValueNow(ValueKey key) {
        if (key == ValueKey.MagnetismPickup)
            return _magnetRange;
        if (key == ValueKey.MagnetismStrength)
            return _magnetStrength;
        return 0;
    }

    public float GetValueBase(ValueKey key) {
        if (key == ValueKey.MagnetismPickup)
            return _magnetRangeBase;
        if (key == ValueKey.MagnetismStrength)
            return _magnetStrengthBase;
        return 0;
    }

    public void Register() {
        UpgradeManagerPlayer.Instance.RegisterValueModifierScript(ValueKey.MagnetismPickup, this);
        UpgradeManagerPlayer.Instance.RegisterValueModifierScript(ValueKey.MagnetismStrength, this);
    }

    public void ReturnValuesToBase() {
        _magnetRange = _magnetRangeBase;
        _magnetStrength = _magnetStrengthBase;
    }
}