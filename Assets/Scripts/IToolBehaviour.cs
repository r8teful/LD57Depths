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
    void ToolStop();
    public GameObject GO { get;}
}