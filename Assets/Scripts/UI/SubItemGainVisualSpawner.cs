using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Spawns a nice lil popup when the submarine inventory gains items
public class SubItemGainVisualSpawner : MonoBehaviour {

    [SerializeField] Transform _popupContainer;
    [SerializeField] Transform _transferVisualWorldDestination;
    [SerializeField] UIInventoryGainPopup _popup; 
    [SerializeField] SubItemTransferVisual _transferVisualPrefab; 

    private Dictionary<ushort, UIInventoryGainPopup> activePopups = new Dictionary<ushort, UIInventoryGainPopup>();
    private Dictionary<ushort, SubItemTransferVisual> activeTransferVisuals= new Dictionary<ushort, SubItemTransferVisual>();
    private InventoryManager _inv;
    private AudioSource _sound;
    private bool _isTransferring;
    private long _quantityToTransferTarget;
    private float _transferSpeed;
    private double _accumulatedQuantity;
    private Coroutine _transferCoroutine;

    public void Init(InventoryManager subInventory) {
        subInventory.OnSlotNew += SlotNew;
        subInventory.OnSlotChanged += SlotChanged;
        ItemTransferManager.OnTransferStart += TransferStart;
        ItemTransferManager.OnTransferCompleteAll += TransferStop;
        ItemTransferManager.TransferSpeedChange += TransferSpeedChange;
        _inv = subInventory;
    }



    private void OnDestroy() {
        _inv.OnSlotNew -= SlotNew;
        _inv.OnSlotChanged -= SlotChanged;
        ItemTransferManager.OnTransferStart -= TransferStart;
        ItemTransferManager.OnTransferCompleteAll -= TransferStop;
        ItemTransferManager.TransferSpeedChange -= TransferSpeedChange;
    }
    private void TransferStart(int quantityToTransfer, float transferSpeed) {
        // Stop any existing transfer cleanly
        if (_isTransferring) {
            TransferStop();
        }

        _quantityToTransferTarget = quantityToTransfer > 0 ? (long)quantityToTransfer : 0L;
        _transferSpeed = transferSpeed;
        _accumulatedQuantity = 0.0;
        _isTransferring = true;

        // Play the looping sound and keep a reference to the AudioSource
        _sound = AudioController.Instance.PlaySound2D("ItemAdd", 0.1f, looping: true) as AudioSource;
      
        // Start the update coroutine
        _transferCoroutine = StartCoroutine(TransferLoop());
    }

    // Call this to stop transferring and stop the sound
    private void TransferStop() {
        if (!_isTransferring) return;

        _isTransferring = false;

        // Stop coroutine if running
        if (_transferCoroutine != null) {
            StopCoroutine(_transferCoroutine);
            _transferCoroutine = null;
        }

        // Stop and null the sound
        if (_sound != null) {
            _sound.DOFade(0, 0.5f).OnComplete(() => {
                _sound.DOKill();
                Destroy(_sound.gameObject);
                _sound = null;
            });
        }

        // reset accumulators if you want
        _accumulatedQuantity = 0.0;
        _quantityToTransferTarget = 0;
    }

    private void TransferSpeedChange(float speedNow) {
        _transferSpeed = speedNow;
    }

    // The running loop that increases quantity and updates pitch
    private IEnumerator TransferLoop() {
        // Robust guard
        if (!_isTransferring) yield break;

        while (_isTransferring) {
            // Increase accumulated quantity by speed * deltaTime
            _accumulatedQuantity += _transferSpeed * Time.deltaTime;

            // If there's a target and we've reached it, clamp and stop
            if (_quantityToTransferTarget > 0 && _accumulatedQuantity >= _quantityToTransferTarget) {
                _accumulatedQuantity = _quantityToTransferTarget;
                // update pitch once more at final quantity
                UpdateSoundPitch((long)_accumulatedQuantity);
                TransferStop();
                yield break;
            }

            // Update the sound pitch based on the integer part of accumulated quantity
            UpdateSoundPitch((long)_accumulatedQuantity);

            yield return null;
        }
    }
    private void UpdateSoundPitch(long quantity) {
        if (_sound == null) return;

        float pitch = QuantityToPitch(quantity);
        // Safety clamp (QuantityToPitch already gives 1..4), but keep a sane upper bound
        pitch = Mathf.Clamp(pitch, 0.1f, 8f);
        _sound.pitch = pitch;
    }
    float QuantityToPitch(long q) {
        if (q <= 0) return 1; // min pitch is 1

        // normalized logarithmic value in [0,1]
        float tRaw = Mathf.Log(1f + q) / Mathf.Log(1f + 10000);
        tRaw = Mathf.Clamp01(tRaw);

        // curveExponent > 1 makes small quantities produce much smaller t
        float t = Mathf.Pow(tRaw, 3);

        return Mathf.Lerp(1, 4, t);
    }

    private void ItemStop(ushort itemID) {
        Debug.Log("item stop!");
        if (!activeTransferVisuals.TryGetValue(itemID, out var visual)) {
            Debug.LogError("STOPPED BUT COULDN'T FIND ID");
            return;
        }
        
        visual.StopVisual();
        activeTransferVisuals.Remove(itemID);
    }
    private void PlayerLayerChange(VisibilityLayerType type) {
        if (type == VisibilityLayerType.Exterior) {
            // stop audio if its playing 
            if (_sound != null && _sound.isPlaying) {
                _sound.DOFade(0, 0.5f).OnComplete(() => {
                    _sound.DOKill();
                    Destroy(_sound.gameObject);
                    _sound = null;
                });
            }
            Destroy(gameObject);
        }
    }
    private void SlotNew(ushort itemId,int newAmount) {
        if (PlayerManager.Instance == null) return;
        if (!PlayerManager.Instance.PlayerLayerController.IsInSub) return;
        if (newAmount <= 0) return;
        Sprite icon = App.ResourceSystem.GetItemByID(itemId).icon;
        var popup = Instantiate(_popup, _popupContainer);
        popup.Init(icon, newAmount, itemId);
        popup.OnDespawned += HandlePopupDespawn;
        activePopups[itemId] = popup;
     }
    private void SlotChanged(ushort itemId,int changeAmount) {
        if (activePopups.TryGetValue(itemId, out UIInventoryGainPopup popup) && popup != null) {
            popup.IncreaseAmount(changeAmount);
        } else {
            SlotNew(itemId,changeAmount);
        }
    }

    private void HandlePopupDespawn(UIInventoryGainPopup popup) {
        popup.OnDespawned -= HandlePopupDespawn;

        var entry = activePopups.FirstOrDefault(kvp => kvp.Value == popup);
        if (entry.Value != null) {
            activePopups.Remove(entry.Key);
        }
    }
}