using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeRecipeUnlockTool", menuName = "ScriptableObjects/Upgrades/UpgradeRecipeUnlockTool")]
public class UpgradeRecipeUnlockTool : UpgradeRecipeUnlock {
    // String is very error prone but who cares lol
    [SerializeField] private string _unlockName;
    protected override string UnlockName => _unlockName;

    public override bool ExecuteRecipe(RecipeExecutionContext context) {
        return context.ToolController.UnlockTool(_unlockName);
    }
}