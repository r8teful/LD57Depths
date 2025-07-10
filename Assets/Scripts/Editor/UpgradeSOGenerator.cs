using System;
using UnityEditor;
using UnityEngine;

public class UpgradeSOGenerator : Editor {
    [MenuItem("Tools/r8teful/GenerateUpgradeSOs")]
    public static void GenerateUpgrades() {
        int treeLength = 12;
        int upgradeTreeTypeCount = Enum.GetValues(typeof(UpgradeTreeType)).Length;
        for (int n = 0; n < upgradeTreeTypeCount; n++) { 
            string upgradeTreeType = ((UpgradeTreeType)n).ToString(); // lol
            for (int i = 1; i <= treeLength; i++) {
                UpgradeRecipeSO upgrade = ScriptableObject.CreateInstance<UpgradeRecipeSO>();
                upgrade.RecipeID = (ushort)(i+200+(treeLength * n));
                upgrade.displayName = $"Upgrade {i}";
                upgrade.description = $"Level {i} upgrade";
                upgrade.icon = null; // Set an icon if desired

                string path = $"Assets/Resources/UpgradeData/{upgradeTreeType}/UpgradeRecipeSO{upgradeTreeType}_LVL{i}.asset";
                AssetDatabase.CreateAsset(upgrade, path);
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Upgrades generated successfully!");
    }
}