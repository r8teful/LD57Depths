using System.Collections;
using UnityEngine;

public static class XPCalculation {
    public static int CalculateXP(ItemData itemData, int quantity) {
        // Get item value
        var v = itemData.itemValue * quantity;

        // Extra modifiers? maybe buffs special items, etc...

        return v;
    }
}