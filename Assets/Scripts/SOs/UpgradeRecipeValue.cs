using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeDataSO", menuName = "ScriptableObjects/UpgradeDataSO", order = 2)]
public class UpgradeRecipeValue : UpgradeRecipeBase {
    public IncreaseType increaseType;
    public float value;

    public override bool ExecuteRecipe(RecipeExecutionContext context) {
        return true; // Handled by events in UpgradeManager.OnUpgradePurchased
        // This is because we'd need mining controller, movement, vision, and more, in the context. It could work, but 
        // gets quite messy to setup the context when calling execute recipe.
    }
}