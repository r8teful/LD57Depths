using UnityEngine;
using System.Collections;


public class SequenceSubGoingUp : Sequencer {
    private bool _introDone;
    public GameObject CutscenePrefab;
    public GameObject WinPrefab;
    private GameObject _instantiatedCutscenePrefab;
    public Transform CanvasMain;
    protected override IEnumerator Sequence() {
        // First 
        StartCoroutine(IntroCutscene());
        yield return new WaitUntil(() => GridManager.Instance.IsWorldGenDone());
        yield return new WaitUntil(() => _introDone);
        Destroy(_instantiatedCutscenePrefab); // Show the game
        yield return new WaitUntil(() => ShipManager.Instance.GetRepairProgress() == 1);
        Debug.Log("Running first cutscene");
        // Second
        yield return Submarine.Instance.Cutscene(0.33f,15);
        yield return new WaitUntil(() => ShipManager.Instance.GetRepairProgress() == 2);
        yield return Submarine.Instance.Cutscene(0.40f,20);
        // Ending cretids?
        yield return new WaitUntil(() => ShipManager.Instance.GetRepairProgress() == 3);
        StartCoroutine(Submarine.Instance.Cutscene(0.2f,20));
        yield return new WaitForSeconds(15);
        Instantiate(WinPrefab, CanvasMain);
    }
    private IEnumerator IntroCutscene() {
        _instantiatedCutscenePrefab = Instantiate(CutscenePrefab, CanvasMain);
        yield return new WaitForSeconds(2); // TODO
        _introDone = true;
    }
}