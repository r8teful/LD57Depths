using Sirenix.OdinInspector;
using UnityEngine;

[CreateAssetMenu(fileName = "ConvertEffect", menuName = "ScriptableObjects/Upgrades/ConvertEffect")]
public class ItemConvertUpgradeEffect : UpgradeEffect {
    [InfoBox("Used for the change status in popup, not in logic")]
    public ItemData itemFrom;
    public int amountFrom;

    [InfoBox("Only this is used in logic")]
    public ItemData itemTo;
    public int amountTo;
    
    public override void Execute(ExecutionContext context) {
        // Items already got removed before hand so simply add the actual items
        SubmarineManager.Instance.SubInventory.AddItem(itemTo.ID,amountTo);
    }

    public override UIExecuteStatus GetExecuteStatus() {
        return new StatChangeStatus("Stone Comversion", amountFrom.ToString(), amountTo.ToString(), false);
    }
}