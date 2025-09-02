using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

// Should handle all things related to moving the submarine
public class SubMovementManager : NetworkBehaviour {
    public static SubMovementManager Instance { get; private set; }
    private int SubPosIndex; // This is stored on server, probably better to be placed in SubmarineManager
    private Transform _sub;
    private void Awake() {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }
    public override void OnStartServer() {
        base.OnStartServer();
        _sub = WorldManager.Instance.GetSubTransform();
    }

    // This will then be called by the UI when all players have confirmed the movement,
    // Then the port will just sync for everyone because the subexterior has a networktransform
    public void MoveSub(int index) {
        _sub.position = new(0,WorldManager.Instance.GetCheckpointYPos(index));
    }
}
