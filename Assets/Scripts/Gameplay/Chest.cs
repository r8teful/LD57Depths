using DG.Tweening;
using UnityEngine;

public class Chest : MonoBehaviour {
    [SerializeField] private Interactable _interactable;
    [SerializeField] private ParticleSystem _openParticles;
    [SerializeField] private ParticleSystem _destroyParticles;
    [SerializeField] private Animator _animator;
    [SerializeField] SaveEntityCallback SaveCallback;
    private bool _hasUsed = false;
    public bool HasUsed {
        get {
            return _hasUsed;
        }
        set {
            _hasUsed = value;
            if (_interactable != null)
                _interactable.CanInteract = !value;
        } 
    } 
    private AudioSource _audio;

    private void Awake() {
        _interactable.OnInteract += OnInteract;
    }
    private void OnDestroy() {
        _interactable.OnInteract -= OnInteract;
    }
   
    private void OnInteract(PlayerManager p) {
        if(_hasUsed) return;
        HasUsed = true;
        //_openParticles.Play();
        _animator.Play("Opening");

        GameSequenceManager.Instance.AddEvent(shouldPause: true,
           onStart: () => {
               _audio = AudioController.Instance.PlaySound2D("Reward2", looping: true);
               p.PlayerReward.GenerateRewardsChest();
               RewardEvents.TriggerOpenChest();
           },
           onFinish: () => {
               _destroyParticles.Play();
               _audio.DOFade(0, 0.5f).OnComplete(()=> { Destroy(_audio); Destroy(gameObject); });
               AudioController.Instance.PlaySound2D("RewardPickup2");
               Debug.Log("On finish!");
               SaveCallback.OnSaveChange(); // this triggers entityManager to save these changes, resulting in the compas to update aswell!
               // This logic is handled by CommitLevelUp below which is called from UI
           }
       );
    }

    public void Init(StructurePlacementResult data) {
        transform.position = new(data.bottomLeftAnchor.x, data.bottomLeftAnchor.y, 0);
    }

    internal void SetInteractable(bool isEntityUsed) {
        HasUsed = isEntityUsed;
    }
}