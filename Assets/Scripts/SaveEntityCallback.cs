using System;
using UnityEngine;

public class SaveEntityCallback : MonoBehaviour {
    [HideInInspector]
    public ulong persistantID;
    public event Action<GameObject,ulong> OnSave;
    public void OnSaveChange() {
        OnSave?.Invoke(gameObject, persistantID);
    }
}