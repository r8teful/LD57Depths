using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Defines how the upgrade tree should look
public class UpgradeTreeDataSO : ScriptableObject {
    Dictionary<int,UpgradeDataSO> upgradeTree;
}