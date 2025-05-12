// ItemData.cs
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDataSO", menuName = "ScriptableObjects/ItemDataSO", order =7)]
public class ItemData : ScriptableObject, IIdentifiable {
    [Header("Info")]
    public string itemName = "New Item";
    public string description = "Item Description";
    public Sprite icon = null;
    public ushort ID;

    [Header("Stacking")]
    public int maxStackSize = 1; // Default to 1 for non-stackable items

    [Header("World Representation")]
    public GameObject droppedPrefab = null; // Prefab instantiated when dropped

    [Header("Usage")]
    public bool isUsable = false;
    public bool isConsumable = true; // Is it used up after one use?
    public int usageCooldown = 0;
    
    ushort IIdentifiable.ID => ID;

    public virtual bool Use(GameObject user) {
        if (isUsable) {
            Debug.LogWarning($"Using {itemName} - Base Use() called. Override in derived class for specific effect!");
            // Base implementation does nothing but indicates it *could* be used.
            // Derived classes will implement actual effects (healing, equipping, etc.)
            return true; // Return true indicate it *was* "used", even if base effect is nothing.
        }
        Debug.LogWarning($"{itemName} is not usable.");
        return false;
    }
}