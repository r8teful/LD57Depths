using System;
using UnityEngine;

// Just a placeholder for now
public class UpgradeManager : StaticInstance<UpgradeManager> {
    public static Action<UpgradeType> UpgradeBought { get; internal set; }

    internal float GetUpgradeValue(UpgradeType miningDamange) {
        throw new NotImplementedException();
    }
}