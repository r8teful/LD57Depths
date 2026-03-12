using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        Directory.CreateDirectory(Path.GetDirectoryName(SaveFilePath));
        Debug.Log("<color=Green>Saved data into: " + SaveFilePath);

        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new QuaternionConverter());
        settings.Converters.Add(new Vector3IntConverter());

        //File.WriteAllText(SaveFilePath, JsonUtility.ToJson(data, prettyPrint: true));
        File.WriteAllText(SaveFilePath, JsonConvert.SerializeObject(data,Formatting.Indented,settings));
    }

    public static bool TryLoad(out SaveData data) {
        if (!File.Exists(SaveFilePath)) {
            data = new SaveData();
            return false;
        }

        var settings = new JsonSerializerSettings();
        settings.Converters.Add(new QuaternionConverter());
        settings.Converters.Add(new Vector3IntConverter());

        var raw = File.ReadAllText(SaveFilePath);
        //var data = JsonUtility.FromJson<SaveData>(raw);
        data = JsonConvert.DeserializeObject<SaveData>(raw,settings);

        // maybe say this for entities!? idk
        // var entities =  JsonConvert.DeserializeObject<List<PersistentEntityData>>(dataString, new JsonSerializerSettings {
        //TypeNameHandling = TypeNameHandling.Auto
        //});

        //data = MigrateIfNeeded(data);  // handle schema upgrades
        CurrentSave = data;
        return true;
    }
}

public class QuaternionConverter : JsonConverter<Quaternion> {
    public override void WriteJson(JsonWriter writer, Quaternion value, JsonSerializer serializer) {
        writer.WriteStartObject();
        writer.WritePropertyName("x"); writer.WriteValue(value.x);
        writer.WritePropertyName("y"); writer.WriteValue(value.y);
        writer.WritePropertyName("z"); writer.WriteValue(value.z);
        writer.WritePropertyName("w"); writer.WriteValue(value.w);
        writer.WriteEndObject();
    }

    public override Quaternion ReadJson(JsonReader reader, Type objectType, Quaternion existingValue, bool hasExistingValue, JsonSerializer serializer) {
        var obj = JObject.Load(reader);
        return new Quaternion(
            obj["x"].Value<float>(),
            obj["y"].Value<float>(),
            obj["z"].Value<float>(),
            obj["w"].Value<float>()
        );
    }
}
public class Vector3IntConverter : JsonConverter<Vector3Int> {
    public override void WriteJson(JsonWriter writer, Vector3Int value, JsonSerializer serializer) {
        writer.WriteStartObject();
        writer.WritePropertyName("x"); writer.WriteValue(value.x);
        writer.WritePropertyName("y"); writer.WriteValue(value.y);
        writer.WritePropertyName("z"); writer.WriteValue(value.z);
        writer.WriteEndObject();
    }

    public override Vector3Int ReadJson(JsonReader reader, Type objectType, Vector3Int existingValue, bool hasExistingValue, JsonSerializer serializer) {
        var obj = JObject.Load(reader);
        return new Vector3Int(
            obj["x"].Value<int>(),
            obj["y"].Value<int>(),
            obj["z"].Value<int>()
        );
    }
}