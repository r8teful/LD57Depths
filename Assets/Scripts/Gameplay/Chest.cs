using DG.Tweening;
using UnityEngine;

public class Chest : MonoBehaviour {
    [SerializeField] private Interactable _interactable;
    [SerializeField] private ParticleSystem _openParticles;
    [SerializeField] private Animator _animator;
    private bool _opened = false;
    private AudioSource _audio;

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

        GameSequenceManager.Instance.AddEvent(shouldPause: true,
           onStart: () => {
               _audio = AudioController.Instance.PlaySound2D("Reward2", looping: true);
               p.PlayerReward.GenerateRewardsChest();
               RewardEvents.TriggerOpenChest();
           },
           onFinish: () => {
               _audio.DOFade(0, 0.5f).OnComplete(()=> Destroy(_audio));
               AudioController.Instance.PlaySound2D("RewardPickup2");
               Debug.Log("On finish!");
               // This logic is handled by CommitLevelUp below which is called from UI
           }
       );
    }

    public void Init(StructurePlacementResult data) {
        transform.position = new(data.bottomLeftAnchor.x, data.bottomLeftAnchor.y, 0);
    }
}