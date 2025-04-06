using UnityEngine;
using System.Collections;

public class SubInside : StaticInstance<SubInside> {
    public SubInteractionState state;
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
    private void StateChange() {
        switch (state) {
            case SubInteractionState.None:
                HideAllInteractions();
                break;
            case SubInteractionState.Upgrade:
                break;
            case SubInteractionState.Ship:
                break;
            case SubInteractionState.Exit:
                break;
            default:
                break;
        }
    }
    private void HideAllInteractions() {

    }
    private void Update() {
        if (Input.GetMouseButton(0)) {
            switch (state) {
                case SubInteractionState.None:
                    break;
                case SubInteractionState.Upgrade:

                    break;
                case SubInteractionState.Ship:
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