using System;
using System.Collections;
using UnityEngine;

public class EventCave : MonoBehaviour {
    [SerializeField] private Interactable _interactable;


    private void Awake() {
        _interactable.OnInteract += OnInteract;
    }
    private void OnDestroy() {
        _interactable.OnInteract -= OnInteract;
    }

    private void OnInteract(PlayerManager p) {

    }
    public void Init(StructurePlacementResult data) {
        transform.position = new(data.bottomLeftAnchor.x, data.bottomLeftAnchor.y, 0);
    }
}