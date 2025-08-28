using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public class UpgradeSOGenerator : Editor {
    //[MenuItem("Tools/r8teful/GenerateUpgradeSOs")]
    public static void GenerateUpgrades() {
        int treeLength = 12;
        //int upgradeTreeTypeCount = Enum.GetValues(typeof(UpgradeTreeType)).Length;
        int upgradeTreeTypeCount = 6;
        for (int n = 0; n < upgradeTreeTypeCount; n++) { 
            //string upgradeTreeType = ((UpgradeTreeType)n).ToString(); // lol
            string upgradeTreeType = "TODO";

            string folderPath = $"Assets/Resources/UpgradeData/{upgradeTreeType}";
            // Ensure folder exists
            if (!AssetDatabase.IsValidFolder(folderPath)) {
                Directory.CreateDirectory(folderPath);
                AssetDatabase.Refresh();
            }

            // Delete existing assets in the folder
            string[] assetPaths = Directory.GetFiles(folderPath, "*.asset");
            foreach (var assetPath in assetPaths) {
                string relativePath = assetPath.Replace("\\", "/"); // Handle Windows paths
                AssetDatabase.DeleteAsset(relativePath);
            }

            for (int i = 1; i <= treeLength; i++) {
                UpgradeRecipeValue upgrade = ScriptableObject.CreateInstance<UpgradeRecipeValue>();
                upgrade.RecipeID = (ushort)(i+200+(treeLength * n));
                upgrade.displayName = $"Upgrade {i}";
                upgrade.description = $"Level {i} upgrade";
                upgrade.icon = null; // Set an icon if desired

                string path = $"{folderPath}/UpgradeRecipeValue{upgradeTreeType}_LVL{i}.asset";
                AssetDatabase.CreateAsset(upgrade, path);
            }
        }
        AssetDatabase.SaveAssets();
        Debug.Log("Upgrades generated successfully!");
    }
}