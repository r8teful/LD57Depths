using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ZoneSO", menuName = "ScriptableObjects/Other/ZoneSO", order = 2)]
public class ZoneSO : ScriptableObject {
    public int ZoneIndex;
    public string ZoneName;
    public List<ItemData> AvailableResources; // Right now we just set it here, but if it's procedural/random, we'd want to make some sort of 
    // runtime check to get this data
}