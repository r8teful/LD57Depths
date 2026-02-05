using UnityEngine;

[RequireComponent(typeof(Interactable))]
public class Chest : MonoBehaviour {
    [SerializeField] private Interactable _interactable;
    [SerializeField] private ParticleSystem _openParticles;
    [SerializeField] private Animator _animator;
    private bool _opened = false;

    private void Awake() {
        _interactable.OnInteract += OnInteract;
    }
    private void OnDestroy() {
        _interactable.OnInteract -= OnInteract;
    }

    private void OnInteract(PlayerManager p) {
        if(_opened) return;
        _interactable.CanInteract = false;
        _openParticles.Play();
        _animator.Play("Opening");

        GameSequenceManager.Instance.AddEvent(
           onStart: () => {
               p.PlayerReward.GenerateRewardsChest();
               RewardEvents.TriggerOpenChest();
           },
           onFinish: () => {
               // This logic is handled by CommitLevelUp below which is called from UI
           }
       );
    }

    public void Init(StructurePlacementResult data) {
        transform.position = new(data.bottomLeftAnchor.x, data.bottomLeftAnchor.y, 0);
    }
}