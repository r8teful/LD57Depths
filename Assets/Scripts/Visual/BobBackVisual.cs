using System;
using System.Collections;
using UnityEngine;

public class BobBackVisual : MonoBehaviour {
    [SerializeField] SpriteRenderer _backSpriteSwimming;
    [SerializeField] SpriteRenderer _backSpriteWalking;
    // Ugly with direct references but eh
    [SerializeField] Sprite _jetpackLaying;
    [SerializeField] Sprite _jetpackStanding;
    [SerializeField] Sprite _oxygenLaying;
    [SerializeField] Sprite _oxygenStanding;
    public void SetVisualNone() {
        _backSpriteSwimming.enabled = false;
        _backSpriteWalking.enabled = false;
        SetBackSprite(null, null);
    }
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

    internal void HandleUpgradeBought(UpgradeRecipeSO upgrade) {
        var upgradeID = upgrade.ID;
        if (upgradeID == ResourceSystem.UpgradeJetpackID) {
            SetBackSprite(_jetpackLaying, _jetpackStanding);
        } else if (upgradeID == ResourceSystem.UpgradeOxygenID) {
            SetBackSprite(_oxygenLaying, _oxygenStanding);
        }
    }

}