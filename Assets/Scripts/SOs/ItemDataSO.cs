// ItemData.cs
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDataSO", menuName = "ScriptableObjects/ItemDataSO", order =7)]
public class ItemData : ScriptableObject, IIdentifiable {
    [HorizontalGroup("Split")]
    [BoxGroup("Split/LeftBox")]
    [HorizontalGroup("Split/LeftBox/Left")]
    [VerticalGroup("Split/LeftBox/Left/2")]
    public string itemName = "New Item";
    
    [VerticalGroup("Split/LeftBox/Left/2")]
    public ushort ID;
    [VerticalGroup("Split/LeftBox/Left/2")]
    public string description = "Item Description";
    [VerticalGroup("Split/LeftBox/Left/1")]
    [PreviewField(75), HideLabel, LabelWidth(0)]
    public Sprite icon = null;

    [HorizontalGroup("Split")]
    [BoxGroup("Split/RightBox")]
    public int maxStackSize = 1; // Default to 1 for non-stackable items

    [Header("World Representation")]
    public GameObject droppedPrefab = null; // Prefab instantiated when dropped

    [VerticalGroup("Split/RightBox/1")]
    public bool isUsable = false;
    [VerticalGroup("Split/RightBox/1")]
    public bool isConsumable = true; // Is it used up after one use?
    [VerticalGroup("Split/RightBox/1")]
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