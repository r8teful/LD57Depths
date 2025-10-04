using System;
using System.Collections;
using UnityEngine;

public class BobBackVisual : MonoBehaviour {
    [SerializeField] SpriteRenderer _backSpriteSwimming;
    [SerializeField] SpriteRenderer _backSpriteWalking;
    public void SetSpriteSwim() {
        _backSpriteSwimming.enabled = true;
        _backSpriteWalking.enabled = false;

    }
    public void SetSpriteWalk() {
        _backSpriteSwimming.enabled = false;
        _backSpriteWalking.enabled = true;
    }
    private void SetBackSprite(Sprite spriteSwimming, Sprite spriteWalking) {
        _backSpriteSwimming.sprite= spriteSwimming;
        _backSpriteWalking.sprite = spriteWalking;
    }
    public void SetSpriteSwim(bool enabled) {
        _backSpriteSwimming.enabled = enabled;
    }
    internal void OnToolUseStart() {
        // Hide the tool from your back if it is laying there
        // TODO 
        // if(UsingToolThatIsOnTheBack) SetSrpiteSwim(false)
    }

    internal void OnToolUseStop() {
        // Put the tool back on the back 
    }
}