using FishNet.Object;
using System;
using UnityEngine;

public class SubHatchVisual : MonoBehaviour {
    [SerializeField] private Interactable _interactable;
    [SerializeField] private Animator _animator;
    private string currentAnimation;

    private void OnEnable() {
        if (_interactable != null) {
            _interactable.OnSetInteractable += OnCloseEnough;
            _interactable.OnCeaseInteractable += OnTooFar;
        }
    }

    private void OnTooFar() {
        HatchClose();
    }

    private void OnCloseEnough() {
        HatchOpen();
    }

    private void HatchOpen() {
        ChangeAnimation("HatchOpen");
    }
    private void HatchClose() {
        ChangeAnimation("HatchClose");
    }
    private void ChangeAnimation(string animationName) {
        if (_animator == null)
            return;
        if (currentAnimation != animationName) {
            currentAnimation = animationName;
            _animator.CrossFade(animationName, 0.2f, 0);
        }
    }

    private void OnDisable() {
        if (_interactable != null) {
            _interactable.OnSetInteractable -= OnCloseEnough;
            _interactable.OnCeaseInteractable -= OnTooFar;
        }
    }
}
