using System;
using System.Collections;
using UnityEngine;

public class MiningDrill : MiningBase, IToolBehaviour {
    
    public bool CanMine { get; private set; }
    public override GameObject GO => gameObject;
    public override float Range {
        get;

        set;
    }

    public override float DamagePerHit {
        get {
            throw new System.NotImplementedException();
        }

        set {
            throw new System.NotImplementedException();
        }
    }

    private void Update() {
        if (_isMining) {
            var pos = _inputManager.GetAimInput();
            //SetCorrectLaserPos(_inputManager.GetMovementInput().x);
            DrillVisual(pos);
        }
    }

    private void DrillVisual(Vector2 pos) {
        Vector2 objectPos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 directionToMouse = (pos - objectPos2D).normalized;
        //transform.LookAt(directionToMouse);
    }
}