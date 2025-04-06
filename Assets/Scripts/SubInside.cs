using UnityEngine;
using System.Collections;
using UnityEngine.UIElements;

public class SubInside : StaticInstance<SubInside> {
    public SubInteractionState state;
    public Renderer Workbench;
    public Renderer Exit;
    public Renderer Control;
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
        UIShopManager.Instance.ShopClose();
        UIShopManager.Instance.ShipClose();
    }
    private void Update() {
        if (!_canInteract) return;
        if (Input.GetMouseButtonDown(0)) {
            switch (state) {
                case SubInteractionState.None:
                    break;
                case SubInteractionState.Upgrade:
                    UIShopManager.Instance.ShopOpen();
                    break;
                case SubInteractionState.Ship:
                    UIShopManager.Instance.ShipOpen();
                    break;
                case SubInteractionState.Exit:
                    Submarine.Instance.ExitSub();
                    break;
                default:
                    break;
            }
        }
    }
}
public enum SubInteractionState {
    None,
    Upgrade,
    Ship,
    Exit
}