using UnityEditor;
using UnityEngine;

public class UpgradeSOGenerator : Editor {
    [MenuItem("Tools/r8teful/GenerateUpgradeSOs")]
    public static void GenerateUpgrades() {
        string upgradeType = "Mining";
        for (int i = 1; i <= 15; i++) {
            UpgradeRecipeSO upgrade = ScriptableObject.CreateInstance<UpgradeRecipeSO>();
            upgrade.RecipeID = (ushort)(i+300+15);
            upgrade.displayName = $"Upgrade {i}";
            upgrade.description = $"Level {i} upgrade";
            upgrade.icon = null; // Set an icon if desired

            string path = $"Assets/Resources/UpgradeData/{upgradeType}/UpgradeRecipeSO{upgradeType}_LVL{i}.asset";
            AssetDatabase.CreateAsset(upgrade, path);
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Upgrades generated successfully!");
    }
}