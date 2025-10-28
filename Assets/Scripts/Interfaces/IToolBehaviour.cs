using UnityEngine;

public interface IToolBehaviour {
    /// <summary>
    /// Called once when the button is pressed
    /// </summary>
    /// <param name="aimPosition"></param>
    /// <param name="controller"></param>
    void ToolStart(InputManager input, ToolController controller);
    /// <summary>
    /// Called ONCE when player has released the mining button
    /// </summary>
    /// <param name="controller"></param>
    void ToolStop(ToolController toolController);
    void ToolAbilityStart(ToolController toolController);
    void ToolAbilityStop(ToolController toolController);
    void Init(NetworkedPlayer owner);
    void OwnerUpdate();
    
    // 'object' allows for any type of data (Vector2, float, custom struct).
    object VisualData { get; }

    public ToolType ToolType { get; }
    public ushort ToolID { get; }
    public GameObject GO { get;}
}

// Order matters and is set as the ID
public enum ToolType {
    Lazer = 0,
    Drill = 1,
    RPG = 2,
    GOD = 99
}