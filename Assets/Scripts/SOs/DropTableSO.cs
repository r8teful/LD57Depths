using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ItemDropInfo {
    public GameObject itemPrefab; // Prefab of the item to drop (must have NetworkObject)
    public int minAmount = 1;
    public int maxAmount = 1;
    [Range(0f, 1f)] public float dropChance = 1.0f; // Chance this specific item drops (0 to 1)
}

// ScriptableObject to define a collection of possible drops
[CreateAssetMenu(fileName = "DropTable", menuName = "ScriptableObjects/Drop Table")]
public class DropTableSO : ScriptableObject {
    public List<ItemDropInfo> drops;
}
