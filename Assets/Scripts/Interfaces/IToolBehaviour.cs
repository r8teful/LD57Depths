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
    MiningToolData GetToolData();
    void InitVisualTool(IToolBehaviour toolBehaviourParent);
    public IToolVisual ToolVisual { get; }
    public ToolType ToolType { get; }
    public ushort ToolID { get; }
    public GameObject GO { get;}
}

// Order matters and is set as the ID
public enum ToolType {
    Drill = 0,
    CleaningTool = 1,
    Lazer = 2,
    RPG = 3,
    GOD = 99
}