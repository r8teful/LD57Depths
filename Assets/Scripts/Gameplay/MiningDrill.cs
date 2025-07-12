using System;
using System.Collections;
using UnityEngine;

public class MiningDrill : MiningBase, IToolBehaviour {
    
    public bool CanMine { get; private set; }
    public override GameObject GO => gameObject;
    public override float Range { get; set; } = 2f;
    public override float DamagePerHit {get; set; } = 5f;

    [SerializeField] private GameObject handVisual;

    private void Update() {
        if (_isMining) {
            var pos = _inputManager.GetAimInput();
            //SetCorrectLaserPos(_inputManager.GetMovementInput().x);
            DrillVisual(pos);
        }
    }
    public override void ToolStart(InputManager input, ToolController controller) {
        base.ToolStart(input, controller);
        handVisual.SetActive(true);
        NetworkedPlayer.LocalInstance.PlayerVisuals.SetBobHand(false);
    }
    public override void ToolStop() {
        base.ToolStop();
        handVisual.SetActive(false);
        // PlayerVisual set sprite to hand
        NetworkedPlayer.LocalInstance.PlayerVisuals.SetBobHand(true);
    }

    private void DrillVisual(Vector2 pos) {
        Vector2 objectPos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 directionToMouse = (pos - objectPos2D).normalized;
        float angle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle );
    }

}