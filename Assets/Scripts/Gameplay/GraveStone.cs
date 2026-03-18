using System.Collections.Generic;
using UnityEngine;

public class GraveStone : Interactable {
    private List<ItemQuantity> _heldIems;
    [SerializeField] private ParticleSystem _destroyParticles;

    protected override void Awake() {
        base.Awake();
        OnInteract += OnInteracted;
    }

    private void OnDestroy() {
        OnInteract -= OnInteracted;
    }

    public void Init(List<ItemQuantity> heldItems) {
        _heldIems = heldItems;
    }

    private void OnInteracted(PlayerManager player) {
        foreach (ItemQuantity item in _heldIems) {
            player.InventoryN.AddItem(item.item.ID, item.quantity);
        }
        _destroyParticles.Play();
        AudioController.Instance.PlaySound2D("popPickupChest", 0.1f);
        base.CanInteract = false;
        Destroy(gameObject,1f);
        if(_spriteRenderer != null) {
            _spriteRenderer.color = new(0, 0, 0, 0); // hide
        }
    }
}