using System.Collections;
using UnityEngine;

public abstract class UpgradeRecipeUnlock : UpgradeRecipeBase {
    protected abstract string UnlockName { get; }
}