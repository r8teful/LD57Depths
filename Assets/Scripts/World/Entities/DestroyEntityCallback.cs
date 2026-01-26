using FishNet.Object;
using System;

public class DestroyEntityCallback : NetworkBehaviour {
    public bool IsDestroyedPermanently = true;
    public event Action<NetworkObject> OnServerStopped;
    public override void OnStopServer() {
        base.OnStopServer();
        //Debug.Log("Onstop server");
        OnServerStopped?.Invoke(NetworkObject);
    }
}