using UnityEngine;

[CreateAssetMenu(fileName = "SubRecipeSO", menuName = "ScriptableObjects/Upgrades/SubRecipeSO", order = 9)]
public class SubRecipeSO : RecipeBaseSO {
    public enum SubUpgradeType {
        Gears,
        Cables,
        Chipp,
        Propeller,
        Hull
    }
    public SubUpgradeType UpgradeType;
    public Sprite[] UpgradeIconSteps;
    public Sprite[] UpgradeExteriorSteps;
    public Sprite[] UpgradeInteriorSteps;
    public override bool ExecuteRecipe(RecipeExecutionContext context) {
        // This is not getting called now read what I wrote in RpcContributeToUpgrade in SubmarineManager
        return true;
    }
}