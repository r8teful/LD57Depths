
using UnityEngine;

[CreateAssetMenu(fileName = "ValueUpgradeEffectSO", menuName = "ScriptableObjects/Upgrades/ValueUpgradeEffectSO")]
public class ValueUpgradeEffectSO : UpgradeEffect {

    public ValueKey valueType;
    public StatModifyType increaseType;
    public float modificationValue;
    public override void Execute(ExecutionContext context) {
       var script = UpgradeManagerPlayer.LocalInstance.Get<IValueModifiable>(valueType);
        ValueModifier modifier = new(modificationValue, valueType, increaseType, this);
        script.ModifyValue(modifier);
        // Programming out here!
    }

    public override StatChangeStatus GetChangeStatus() {
        var script = UpgradeManagerPlayer.LocalInstance.Get<IValueModifiable>(valueType);
        var valueNow = script.GetValue(valueType);
        
        var newV = UpgradeCalculator.CalculateUpgradeChange(valueNow, increaseType, modificationValue);
        // make it a procent change duh
        return new("todo",valueNow,newV,true);
    }
}