using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DialogueEventSO", menuName = "ScriptableObjects/DialogueEventSO", order = 99)]
public class DialogueEventSO : ScriptableObject {
    public enum InterruptBehaviour {
        Skip,
        ResumeAfter,
        Uninterruptable,
    }

    public InterruptBehaviour interruptBehaviour = InterruptBehaviour.Skip;
    public List<string> lines = new List<string>();
}
