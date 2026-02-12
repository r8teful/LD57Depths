using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "EventCaveSO", menuName = "ScriptableObjects/Other/EventCaveSO", order = 8)]

public class EventCaveSO : ScriptableObject {

    [TextArea]
    public string Descritpion;
    public List<EventOption> Options;
}

[Serializable]
public class EventOption {
    public string OptionText; // 
    public List<EventResult> result; 
    public string ResultText; // 
}
[Serializable]
public class EventResult {
    public List<UpgradeEffect> effects; 
}