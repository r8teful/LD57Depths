using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Sequencer : MonoBehaviour
{
    [SerializeField]
    protected List<DialogueEventSO> dialogueEvents = default;

    [SerializeField]
    private float waitBeforeNext = 0f;

    [SerializeField]
    private GameObject activateWhenComplete = default;

    private void Start() {
        StartCoroutine(SequenceThenActivateNext());
    }

    private IEnumerator SequenceThenActivateNext() {
        yield return Sequence();

        yield return new WaitForSeconds(waitBeforeNext);
        //yield return new WaitUntil(() => DialogueManager.Instance.NoDialoguePlaying);
        //yield return new WaitForSeconds(0.5f);
        yield return new WaitForEndOfFrame();
        if (activateWhenComplete != null) {
            activateWhenComplete.SetActive(true);
        }
    }

    protected virtual IEnumerator Sequence() { yield break; }
}
