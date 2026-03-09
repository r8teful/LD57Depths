using r8teful;
using System;
using System.IO;
using UnityEngine;
public class SaveManager {
    const string SAVE_FILE = "GameSave.json";
    static string SaveFilePath =>
       Path.Combine(SavePathResolver.GetSharedSavePath(), SAVE_FILE);

    public static SaveData CurrentSave { get; private set; }

    public static void Save(SaveData data) {
        data.lastSavedUtc = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        data.gameVersion = Application.version;

#if DEMO_BUILD
        data.buildType = "demo";
#else
        data.buildType = "full";
#endif

        Directory.CreateDirectory(Path.GetDirectoryName(SaveFilePath)!);
        File.WriteAllText(SaveFilePath, JsonUtility.ToJson(data, prettyPrint: true));
    }

    public static SaveData Load() {
        if (!File.Exists(SaveFilePath))
            return new SaveData();

        var raw = File.ReadAllText(SaveFilePath);
        var data = JsonUtility.FromJson<SaveData>(raw);

        //data = MigrateIfNeeded(data);  // handle schema upgrades
        CurrentSave = data;
        return data;
    }

}
