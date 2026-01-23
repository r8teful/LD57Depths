using UnityEngine;

public interface IExecutable {
    public abstract void Execute(ExecutionContext context); // AGH context here is fucked
    // So much old code is from this RecipeExecutionContext that can be removed
    // The only thing that still uses it is the sub fixing recipes, but we
    // should honestly remove it.
    
    // We need this interface because a recipe execution and an upgrade effect 
    // both execute on something. This way we can use it in upgrade rewards
}
public class ExecutionContext {

    public readonly NetworkedPlayer Player;
    public readonly GameObject Source; // The originating object 

    public ExecutionContext(NetworkedPlayer player, GameObject source = null) {
        Player = player;
        Source = source;
    }
    public static ExecutionContext FromPlayer(NetworkedPlayer player) {
        return new ExecutionContext(player);
    }
    public static ExecutionContext FromObject(GameObject obj) {
        return new ExecutionContext(null, obj);
    }
}