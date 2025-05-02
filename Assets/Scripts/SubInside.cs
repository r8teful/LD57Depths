using UnityEngine;
using System.Collections;

public class SubInside : StaticInstance<SubInside> {
    public SubInteractionState state;
    public Renderer Workbench;
    public Renderer Exit;
    public Renderer Control;
    public SpriteRenderer Background;
    public int backgroundLevel;
    private bool _canInteract;
    private void Start() {
        Workbench.material = new Material(Workbench.material);
        Exit.material = new Material(Exit.material);
        Control.material = new Material(Control.material);
    }
    public void EnterUpgrade() {
        state = SubInteractionState.Upgrade;
        StateChange();
    }
    public void EnterShipControll() {
        state = SubInteractionState.Ship;
        StateChange();
    }
    public void EnterExit() {
        state = SubInteractionState.Exit;
        StateChange();
    }

    public void ExitCollider() {
        state = SubInteractionState.None;
        StateChange();

    }
    public void PlayerEntered() {
        _canInteract = false;
        StartCoroutine(ExitCooldown());
    }
    private IEnumerator ExitCooldown() {
        yield return new WaitForSeconds(0.5f);
        _canInteract = true;
    }
    private void StateChange() {
        switch (state) {
            case SubInteractionState.None:
                HideAllInteractions();
                break;
            case SubInteractionState.Upgrade:
                Workbench.material.SetInt("_Enabled", 1);
                break;
            case SubInteractionState.Ship:
                Control.material.SetInt("_Enabled", 1);
                break;
            case SubInteractionState.Exit:
                Exit.material.SetInt("_Enabled", 1);
                break;
            default:
                break;
        }
    }
    private void HideAllInteractions() {
        Control.material.SetInt("_Enabled", 0);
        Workbench.material.SetInt("_Enabled", 0);
        Exit.material.SetInt("_Enabled", 0);
        ShipManager.Instance.ShopClose();
        ShipManager.Instance.ShipClose();
    }
    private void Update() {
        if (!_canInteract) return;
        if (Input.GetMouseButtonDown(0)) {
            switch (state) {
                case SubInteractionState.None:
                    break;
                case SubInteractionState.Upgrade:
                    ShipManager.Instance.ShopOpen();
                    break;
                case SubInteractionState.Ship:
                    ShipManager.Instance.ShipOpen();
                    break;
                case SubInteractionState.Exit:
                    //Submarine.Instance.ExitSub();
                    break;
                default:
                    break;
            }
        }
        if (Input.GetKeyDown(KeyCode.Escape)) {
            switch (state) {
                case SubInteractionState.None:
                    break;
                case SubInteractionState.Upgrade:
                    ShipManager.Instance.ShopClose();
                    break;
                case SubInteractionState.Ship:
                    ShipManager.Instance.ShipClose();
                    break;
                case SubInteractionState.Exit:
                    break;
                default:
                    break;
            }
        }
    }
    public void IncreaseBackgroundLevel() {
        backgroundLevel++;
        if(backgroundLevel == 1) {
            Background.sprite = Resources.Load<Sprite>("UI/BackgroundLevel2");
        } else if(backgroundLevel == 2) {
            Background.sprite = Resources.Load<Sprite>("UI/BackgroundLevel3");
        }
    }
}
public enum SubInteractionState {
    None,
    Upgrade,
    Ship,
    Exit
}