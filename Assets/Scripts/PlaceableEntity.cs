using System;
using System.Collections;
using UnityEngine;

// A preview entity 
public class PlaceableEntity : MonoBehaviour {
    public Collider2D PlacementCollider;
    public EntityBaseSO EntityData; // The actual entity that will get spawned when placed succefully
    [SerializeField] private SpriteRenderer _spriteRenderer;
    internal void SetColor(Color color) {
        _spriteRenderer.color = color;
    }
}