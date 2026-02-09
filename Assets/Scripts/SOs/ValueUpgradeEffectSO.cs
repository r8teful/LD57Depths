
using UnityEngine;

[CreateAssetMenu(fileName = "ValueUpgradeEffectSO", menuName = "ScriptableObjects/Upgrades/ValueUpgradeEffectSO")]
public class ValueUpgradeEffectSO : UpgradeEffect {

    public ValueKey valueType;
    public StatModifyType increaseType;
    public float modificationValue;
    public override void Execute(ExecutionContext context) {
       var script = UpgradeManagerPlayer.Instance.Get<IValueModifiable>(valueType);
        ValueModifier modifier = new(modificationValue, valueType, increaseType, this);
        script.ModifyValue(modifier);
        // Programming out here!
    }

    public override StatChangeStatus GetChangeStatus() {
        var script = UpgradeManagerPlayer.Instance.Get<IValueModifiable>(valueType);
        var valueBase = script.GetValueBase(valueType);
        var valueNow = script.GetValueNow(valueType);
        var valueNext = UpgradeCalculator.CalculateUpgradeChange(valueNow, increaseType, modificationValue);
        
        // make it a procent change duh
        float percentNow = valueNow / (float)valueBase;
        float percentNext = valueNext / (float)valueBase;

        int currentProcent = Mathf.RoundToInt(percentNow * 100f);
        int nextProcent = Mathf.RoundToInt(percentNext * 100f);
        return new("todo", $"{currentProcent}%", $"{nextProcent}%", true);
    }
}