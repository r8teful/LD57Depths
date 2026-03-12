using r8teful;
using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;
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

        Directory.CreateDirectory(Path.GetDirectoryName(SaveFilePath));
        Debug.Log("<color=Green>Saved data into: " + SaveFilePath);
        //File.WriteAllText(SaveFilePath, JsonUtility.ToJson(data, prettyPrint: true));
        File.WriteAllText(SaveFilePath, JsonConvert.SerializeObject(data,Formatting.Indented));
    }

    public static bool TryLoad(out SaveData data) {
        if (!File.Exists(SaveFilePath)) {
            data = new SaveData();
            return false;
        }

        var raw = File.ReadAllText(SaveFilePath);
        //var data = JsonUtility.FromJson<SaveData>(raw);
        data = JsonConvert.DeserializeObject<SaveData>(raw);

        //data = MigrateIfNeeded(data);  // handle schema upgrades
        CurrentSave = data;
        return true;
    }

}
