using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChestRewardEffect : IExecutable {
    private readonly List<ItemQuantity> itemsToGain;
    private readonly int xp;

    public List<ItemQuantity> GetRewards() => itemsToGain;

    public ChestRewardEffect(List<ItemQuantity> itemsToGain, int xpToGain) {
        this.itemsToGain = itemsToGain;
        this.xp = xpToGain;
    }

    public void Execute(ExecutionContext context) {
        foreach (var item in itemsToGain) {
            context.Player.InventoryN.AddItem(item.item.ID, item.quantity);
        }
        RewardEvents.TriggerGainXP(xp);
    }

}