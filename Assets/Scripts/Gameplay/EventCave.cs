using UnityEngine;

public class EventCave : MonoBehaviour {
    [SerializeField] private Interactable _interactable;
    private bool _hasUsed;

    private void Awake() {
        _interactable.OnInteract += OnInteract;
    }
    private void OnDestroy() {
        _interactable.OnInteract -= OnInteract;
    }

    private void OnInteract(PlayerManager p) {
        if (_hasUsed) return;
        _hasUsed = true;
        _interactable.CanInteract = false;
        GameSequenceManager.Instance.AddEvent(shouldPause: true,
         onStart: () => {
             p.PlayerReward.GenerateRewardCave();
             RewardEvents.TriggerOpenCave();
         }
     );
    }
    public void Init(StructurePlacementResult data) {
        transform.position = new(data.bottomLeftAnchor.x, data.bottomLeftAnchor.y, 0);
    }
}