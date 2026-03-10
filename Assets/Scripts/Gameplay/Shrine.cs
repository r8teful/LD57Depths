using DG.Tweening;
using UnityEngine;

public class Shrine : MonoBehaviour {
    [SerializeField] private Interactable _interactable;
    private bool _hasUsed = false;
    private AudioSource _audio;

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
             _audio = AudioController.Instance.PlaySound2D("Reward2", looping: true);
             p.PlayerReward.GenerateRewardsShrine();
             RewardEvents.TriggerOpenShrine();
         },
           onFinish: () => {
               _audio.DOFade(0, 0.5f).OnComplete(() => Destroy(_audio));
               AudioController.Instance.PlaySound2D("RewardPickup2");
               // This logic is handled by CommitLevelUp below which is called from UI
           }
     );
    }
    public void Init(StructurePlacementResult data) {
        transform.position = new(data.bottomLeftAnchor.x, data.bottomLeftAnchor.y, 0);
    }
}