using UnityEngine;

public class ItemDatabaseBootstrap {
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void PreloadDatabase() {
        Debug.Log("Preloading database");
        var _ = ItemDatabase.Instance;
    }
}