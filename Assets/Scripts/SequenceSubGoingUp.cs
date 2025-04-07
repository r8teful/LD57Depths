using UnityEngine;
using System.Collections;


public class SequenceSubGoingUp : Sequencer {
    protected override IEnumerator Sequence() {
        // First 
        yield return new WaitUntil(() => ShipManager.Instance.GetRepairProgress() == 1);
        Debug.Log("Running first cutscene");
        // Second
        yield return Submarine.Instance.Cutscene(0.33f,20);
        yield return new WaitUntil(() => ShipManager.Instance.GetRepairProgress() == 2);
        // Ending cretids?
        yield return new WaitUntil(() => ShipManager.Instance.GetRepairProgress() == 3);
    }
}