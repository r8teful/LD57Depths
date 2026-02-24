
public class ItemGainRewardEffect : CaveAction {
    public ItemData Item;
    public int Quantity;
    public override void Execute(ExecutionContext context) {
        if (context.Player == null) return;
        context.Player.GetInventory().AddItem(Item.ID, Quantity);
        
    }

    public override UIExecuteStatus GetExecuteStatus() {
        return new ItemGainStatus(new(Item, Quantity));
    }
}