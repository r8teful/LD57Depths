using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "SubRecipeSO", menuName = "ScriptableObjects/Upgrades/SubRecipeSO", order = 9)]
public class SubRecipeSO : RecipeBaseSO {
    public Sprite[] UpgradeIconSteps;
    public override bool ExecuteRecipe(RecipeExecutionContext context) {
        // TODO, this will upgrade the ship I guess
        throw new System.NotImplementedException();
    }
}