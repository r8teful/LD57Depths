// ItemData.cs
using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemDataSO", menuName = "ScriptableObjects/ItemDataSO", order =7)]
public class ItemData : ScriptableObject, IIdentifiable {
    [BoxGroup("Identification")]
    [HorizontalGroup("Identification/Left")]
    [VerticalGroup("Identification/Left/2")]
    public string itemName = "New Item";
    
    [VerticalGroup("Identification/Left/2")]
    public ushort ID;
    [VerticalGroup("Identification/Left/2")]
    public string description = "Item Description";
    [VerticalGroup("Identification/Left/1")]
    [PreviewField(75), HideLabel, LabelWidth(0)]
    public Sprite icon = null;

    //[VerticalGroup("Gamepaly")]
    [BoxGroup("Gameplay")]
    [HorizontalGroup("Gameplay/Left")]
    [VerticalGroup("Gameplay/Left/1")]
    public float droppGravityScale = 0;
    [VerticalGroup("Gameplay/Left/1")]
    public float linearDamping = 0.1f;
    [VerticalGroup("Gameplay/Left/1")]
    public int itemValue = 0; // Used for upgrade calculations

    [VerticalGroup("Gameplay/Left/2")]
    [PreviewField(75), HideLabel, LabelWidth(0)]
    public Sprite droppSprite = null;
    ushort IIdentifiable.ID => ID;

}