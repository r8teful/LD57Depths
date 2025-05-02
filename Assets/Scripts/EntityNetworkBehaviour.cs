using FishNet.Object;
using System;
public class EntityNetworkBehaviour : NetworkBehaviour {
    public event Action<NetworkObject> OnAnyNetworkObjectStopped;

    // instance event on this particular object
    public event Action OnStopped;

    // override the built-in Fish-Net callback
    public override void OnStopServer() {
        base.OnStopServer();

        // let subscribers on this instance know
        OnStopped?.Invoke();

        // let anyone listening for any object know
        OnAnyNetworkObjectStopped?.Invoke(NetworkObject);
    }
    
}