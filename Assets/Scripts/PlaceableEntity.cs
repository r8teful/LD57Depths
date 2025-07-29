using System;
using System.Collections;
using UnityEngine;
public class PlaceableEntity : MonoBehaviour {

    public EntityBaseSO EntityData;
    public Collider2D PlacementCollider;
    [SerializeField] private SpriteRenderer _spriteRenderer;
    internal void SetColor(Color color) {
        _spriteRenderer.color = color;
    }
}