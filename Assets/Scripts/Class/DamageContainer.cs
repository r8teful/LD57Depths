using UnityEngine;

public class DamageContainer {
    public float damage;
    public bool crit;
    public Vector3Int tile;
    public Vector2 exactHitPoint;
    public Vector2 hitDirection;

    public DamageContainer() {
    }

    public DamageContainer(float damage, bool crit, Vector3Int tile, Vector2 hitDirection) {
        this.damage = damage;
        this.crit = crit;
        this.tile = tile;
        this.hitDirection = hitDirection;
    }
}