using UnityEngine;

public class PlayerUISpawner : MonoBehaviour, IPlayerModule {
    public int InitializationOrder => 11;

    public UIManager UiManager { get; private set; }

    public void InitializeOnOwner(PlayerManager playerParent) {
        UiManager = Instantiate(App.ResourceSystem.GetPrefab<UIManager>("UIManager"));
        UiManager.Init(playerParent, gameObject); // UI needs inv to suscribe to events and display it 
    }
}