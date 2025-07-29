using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "CraftingEntitySO", menuName = "ScriptableObjects/Crafting/CraftingEntitySO", order = 9)]
public class CraftingEntitySO : CraftingRecipeSO {
    public EntityBaseSO EntityToCraft;
    public override bool ExecuteRecipe(RecipeExecutionContext context) {
        Debug.LogError("Should not call execute Recipe on a Crafting Entity, use ExecuteRecipeRoutine");
        return false; 
    }

    public override IEnumerator ExecuteRecipeRoutine(RecipeExecutionContext context) {
        context.Success = false;

        bool buildTaskFinished = false;
        bool buildResult = false;

        // Subscribe to the completion event
        // The lambda expression captures the 'buildTaskFinished' and 'buildResult' variables
        System.Action<bool> onComplete = (success) => {
            buildResult = success;
            buildTaskFinished = true;
        };
        BuildingManager.Instance.OnBuildAttemptComplete += onComplete;

        BuildingManager.Instance.EnterBuilding(EntityToCraft.ID); 

        // Wait until the event is fired
        yield return new WaitUntil(() => buildTaskFinished);

        // Unsubscribe to prevent memory leaks
        BuildingManager.Instance.OnBuildAttemptComplete -= onComplete;

        context.Success = buildResult;

        // Done, return to the CraftingComponent
        yield break;
    }
}