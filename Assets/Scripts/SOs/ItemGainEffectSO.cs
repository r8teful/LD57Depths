using System.Collections;
using UnityEngine;

public class ItemGainEffectSO : UpgradeEffect {
    public override void Execute(ExecutionContext context) {
        
    }

    public override StatChangeStatus GetChangeStatus() {
        return new();
    }
}