using DG.Tweening;
using UnityEngine;

public class Shrine : MonoBehaviour {
    [SerializeField] private Interactable _interactable;
    [SerializeField] private ParticleSystem _usedParticle;
    [SerializeField] private ParticleSystem _passiveParticles;
    [SerializeField] private Sprite _usedSprite;
    [SerializeField] private SpriteRenderer _sprite;
    private bool _hasUsed = false;
    public bool HasUsed {
        get {
            return _hasUsed;
        }
        set {
            _hasUsed = value;
            if (_interactable != null)
                _interactable.CanInteract = !value;
            SetVisualState(!value);
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
        if (_hasUsed) return;
        HasUsed = true;
        GameSequenceManager.Instance.AddEvent(shouldPause: true,
         onStart: () => {
             _audio = AudioController.Instance.PlaySound2D("Reward2", looping: true);
             p.PlayerReward.GenerateRewardsShrine();
             RewardEvents.TriggerOpenShrine();
         },
           onFinish: () => {
               _audio.DOFade(0, 0.5f).OnComplete(() => Destroy(_audio));
               AudioController.Instance.PlaySound2D("RewardPickup2");
               _usedParticle.Play(); // cool particles
               SetVisualState(isEnabled: false);
           }
     );
    }
    private void SetVisualState(bool isEnabled) {
        if (isEnabled) {
            // set by default
        } else {
            _sprite.sprite = _usedSprite;
            _sprite.color = Color.white;
            _passiveParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
    public void Init(StructurePlacementResult data) {
        transform.position = new(data.bottomLeftAnchor.x, data.bottomLeftAnchor.y, 0);
    }

    internal void SetInteractable(bool isEntityUsed) {
        HasUsed = isEntityUsed;
    }
}