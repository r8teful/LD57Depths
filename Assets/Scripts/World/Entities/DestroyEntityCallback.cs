using System;
using UnityEngine;

public class DestroyEntityCallback : MonoBehaviour {
    public bool IsDestroyedPermanently = true;
    public event Action<GameObject> OnDestroyed;
    public void OnDestroy() {
        OnDestroyed?.Invoke(gameObject);
    }
}