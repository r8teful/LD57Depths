
using UnityEngine;

[CreateAssetMenu(fileName = "ValueUpgradeEffectSO", menuName = "ScriptableObjects/Upgrades/ValueUpgradeEffectSO")]
public class ValueUpgradeEffectSO : UpgradeEffect {

    public ValueKey valueType;
    public StatModifyType increaseType;
    public float modificationValue;
    public override void Execute(ExecutionContext context) {
       var script = PlayerManager.Instance.UpgradeManager.Get(valueType);
        ValueModifier modifier = new(modificationValue, valueType, increaseType, this);
        if(script == null) {
            Debug.LogError("couldn't find IValueModifable script, did you register it???");
            return;
        }
        script.ModifyValue(modifier);
        // Programming out here!
    }

    public override UIExecuteStatus GetExecuteStatus() {
        var script = PlayerManager.Instance.UpgradeManager.Get(valueType);
        if(script == null) {
            Debug.LogWarning("coudn't find script with valueType: " + valueType);
            return null;
        }
        ValueModifier modifier = new(modificationValue, valueType, increaseType, this);
        DisplayType type = ResourceSystem.GetDisplayType(valueType);
        if(type == DisplayType.Absolute) {
            return modifier.GetStatusAbsolute(script);
        } else {
            return modifier.GetStatusProcent(script);
        }
    }
}