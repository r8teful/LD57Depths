using Assets.SimpleLocalization.Scripts;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ZoneSO", menuName = "ScriptableObjects/Other/ZoneSO", order = 2)]
public class ZoneSO : ScriptableObject {
    public int ZoneIndex;
    public string ZoneName;
    public string ZoneNameID;
    public List<ItemData> AvailableResources; // Right now we just set it here, but if it's procedural/random, we'd want to make some sort of 
    // runtime check to get this data
    
    public string GetLocalizedZoneName() {
        if (ZoneNameID == null || ZoneNameID == string.Empty) {
            LocalizationManager.TryLocalize(ZoneNameID, out var localZone);
            var s = localZone;
            if (s == null || s == string.Empty) {
                s = ZoneName;
            }
            return s;
        }
        return ZoneName;
    }
    public string GetLocalizedZoneCantGetDesc() {
        if (ZoneNameID != null || ZoneNameID != string.Empty) {
            LocalizationManager.TryLocalize($"{ZoneNameID}.D", out var localZone);
            var s = localZone;
            if (s == null || s == string.Empty) {
                s = "If you see this text its a bug";
            }
            return s;
        }
        return "If you see this text its a bug";
    }
}