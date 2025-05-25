using System.Collections;
using UnityEngine;

public interface IMiningBehaviour {
    /// <summary>
    /// Called each frame while the approriate button is pressed
    /// </summary>
    /// <param name="aimPosition"></param>
    /// <param name="controller"></param>
    void MineStart(InputManager input, MiningController controller);
    /// <summary>
    /// Called ONCE when player has released the mining button
    /// </summary>
    /// <param name="controller"></param>
    void MineStop(MiningController controller);
}

public class DrillBehavior : IMiningBehaviour {
    public void MineStart(InputManager input, MiningController controller) {
       // todo
    }

    public void MineStop(MiningController controller) {
        return;
    }
}