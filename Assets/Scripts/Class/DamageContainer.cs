using UnityEngine;

public class DamageContainer {
    public float damage;
    public bool crit;
    public Vector3Int tile;

    public DamageContainer() {
    }

    public DamageContainer(float damage, bool crit, Vector3Int tile) {
        this.damage = damage;
        this.crit = crit;
        this.tile = tile;
    }
}