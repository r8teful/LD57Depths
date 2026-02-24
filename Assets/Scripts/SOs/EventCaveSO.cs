using System;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "EventCaveSO", menuName = "ScriptableObjects/Other/EventCaveSO", order = 8)]

// Its a quite simple hierarchy: You've got the event, then you got a list of options for the event
// And those options each could have a list of results, and each of those results could have several effects.
// Like one option could be "10% chance to fail, 90% chance to gain 200 gold", then those two would be different results, each with their own effects
// The actual results would say how they get executed, and how they get displayed in the UI. 
public class EventCaveSO : ScriptableObject, IIdentifiable {
    [SerializeField] private ushort id;
    public ushort ID => id;
    [TextArea]
    public string Description;
    public List<EventOption> Options;
    public EventCaveOutcomeType Type;

    internal void TryGenerateOutcome() {
        // Generate outcome 

    }
}
// Will optiosn ahve a chance? Will we need to specify that here?
[Serializable]
public class EventOption {
    // Conditions?
    public string OptionText; // 
    [SerializeReference]
    public List<CaveAction> action; // The outcome 
}
[Serializable]
public abstract class CaveAction : IExecutable {
    public string ResultText; 
    public abstract void Execute(ExecutionContext context);
    public abstract UIExecuteStatus GetExecuteStatus();
}

public enum EventCaveOutcomeType {
    Item,
    Stat
}
public enum EventOptionType {
    RandomOption,
    Stat
}

[Serializable]
public class RandomOutcomeAction : CaveAction {
    [Serializable]
    public struct RandomEntry {
        public float weight; // Probability weight
        [SerializeReference] public List<CaveAction> actionsToRun; // Nested actions!
    }

    public List<RandomEntry> possibleOutcomes;

    public override void Execute(ExecutionContext player) {
        // 1. Calculate total weight
        float totalWeight = 0;
        foreach (var entry in possibleOutcomes) totalWeight += entry.weight;

        // 2. Pick a random number
        float roll = UnityEngine.Random.Range(0, totalWeight);

        // 3. Find which entry we hit
        float currentWeight = 0;
        foreach (var entry in possibleOutcomes) {
            currentWeight += entry.weight;
            if (roll <= currentWeight) {
                // Execute all actions in this specific outcome
                foreach (var action in entry.actionsToRun) {
                    action.Execute(player);
                }
                return;
            }
        }
    }

    public override UIExecuteStatus GetExecuteStatus() {
        throw new NotImplementedException();
    }
}
[Serializable]
public class ExitCaveAction : CaveAction {
    public override void Execute(ExecutionContext context) {
        throw new NotImplementedException();
    }

    public override UIExecuteStatus GetExecuteStatus() {
        throw new NotImplementedException();
    }
}