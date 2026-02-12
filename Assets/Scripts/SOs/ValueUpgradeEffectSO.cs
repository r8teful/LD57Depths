
using UnityEngine;

[CreateAssetMenu(fileName = "ValueUpgradeEffectSO", menuName = "ScriptableObjects/Upgrades/ValueUpgradeEffectSO")]
public class ValueUpgradeEffectSO : UpgradeEffect {

    public ValueKey valueType;
    public StatModifyType increaseType;
    public float modificationValue;
    public override void Execute(ExecutionContext context) {
       var script = UpgradeManagerPlayer.Instance.Get<IValueModifiable>(valueType);
        ValueModifier modifier = new(modificationValue, valueType, increaseType, this);
        if(script == null) {
            Debug.LogError("couldn't find IValueModifable script, did you register it???");
            return;
        }
        script.ModifyValue(modifier);
        // Programming out here!
    }

    public override StatChangeStatus GetChangeStatus() {
        var script = UpgradeManagerPlayer.Instance.Get<IValueModifiable>(valueType);
        if(script == null) {
            Debug.LogWarning("coudn't find script with valueType: " + valueType);
            return new();
        }
        ValueModifier modifier = new(modificationValue, valueType, increaseType, this);
        return modifier.GetStatus(script);
    }
}