using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Central part of the game that handles what sequences should happen and so that not several happen at once
public class GameSequenceManager : StaticInstance<GameSequenceManager> {

    public class QueuedEvent {
        public Action OnStart;  // What happens when this event begins
        public Action OnFinish; // What happens when this event is officially done
    }

    private Queue<QueuedEvent> eventQueue = new Queue<QueuedEvent>();
    private QueuedEvent currentEvent;


    /// <summary>
    /// Other scripts call this to request an interruption (Level up, Chest, etc)
    /// </summary>
    public void AddEvent(Action onStart, Action onFinish = null) {
        QueuedEvent newEvent = new QueuedEvent {
            OnStart = onStart,
            OnFinish = onFinish
        };

        eventQueue.Enqueue(newEvent);
        
        // If nothing is happening right now, start this new event immediately
        if (currentEvent == null) {
            PlayNextEvent();
        }
    }

    /// <summary>
    /// The UI (or whatever is handling the interaction) calls this when the player is done.
    /// </summary>
    public void AdvanceSequence() {
        // Finish the current event
        currentEvent?.OnFinish?.Invoke();
        // Note: We don't have a reference to the specific current event object here because we just care that the sequence is moving forward.
        currentEvent = null;
        // Check if there are more events waiting
        PlayNextEvent();
    }

    private void PlayNextEvent() {
        if (eventQueue.Count > 0) {

            currentEvent = eventQueue.Dequeue();

            // Pausing logic usually happens here (or inside OnStart)
            Time.timeScale = 0;

            currentEvent.OnStart?.Invoke();
        } else {
            Time.timeScale = 1;
        }
    }
}