using System.Collections;
using UnityEngine;

public class MiningDrill : MiningBase, IToolBehaviour {
    
    public bool CanMine { get; private set; }

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

    public override void ToolHide() {
        GetComponent<PlayerVisualHandler>().DrillHide();
    }

    public override void ToolShow() {
        GetComponent<PlayerVisualHandler>().DrillShow();
    }

    private void Update() {
        if (_isMining) {
            var pos = _inputManager.GetAimInput();
            //SetCorrectLaserPos(_inputManager.GetMovementInput().x);
            //LaserVisual(pos);
        }
    }
}