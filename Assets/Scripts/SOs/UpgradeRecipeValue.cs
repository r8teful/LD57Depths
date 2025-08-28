using UnityEngine;

[CreateAssetMenu(fileName = "UpgradeRecipeValue", menuName = "ScriptableObjects/Upgrades/UpgradeRecipeValue", order = 2)]
public class UpgradeRecipeValue : UpgradeRecipeBase {
    public IncreaseType increaseType;
    public float value;

    public override bool ExecuteRecipe(RecipeExecutionContext context) {
        if(context.Source.TryGetComponent<IValueUpgradeable>(out var upgradeableSystem)){
            upgradeableSystem.ApplyValueUpgrade(this);
        } else {
            Debug.LogWarning($"The system '{context.Source}' cannot have a ValueUpgradeSO applied to it.");
        }
        return true; // Handled by events in UpgradeManager.OnUpgradePurchased
        // This is because we'd need mining controller, movement, vision, and more, in the context. It could work, but 
        // gets quite messy to setup the context when calling execute recipe.
    }
}