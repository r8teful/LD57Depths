using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;
public class SOEditorWindow : OdinMenuEditorWindow {
    [MenuItem("Tools/r8teful/SOEditor")]
    private static void Open() {
        var window = GetWindow<SOEditorWindow>();
        window.position = GUIHelper.GetEditorWindowRect().AlignCenter(800, 500);
    }

    protected override OdinMenuTree BuildMenuTree() {
        var tree = new OdinMenuTree(true);
        tree.DefaultMenuStyle.IconSize = 28.00f;
        tree.Config.DrawSearchToolbar = true;
        // Load and sort assets for each category
        AddSortedAssets<ItemData>(tree, "Items", "Assets/Resources/ItemData");
        AddSortedAssets<RecipeBaseSO>(tree, "Recipies", "Assets/Resources/RecipeData");
        AddSortedAssets<RecipeBaseSO>(tree, "RecipiesUpgrade", "Assets/Resources/UpgradeData");
        AddSortedAssets<EntityBaseSO>(tree, "Entities", "Assets/Resources/EntityData");
        AddSortedAssets<TileSO>(tree, "Tiles", "Assets/Resources/TileData");

        // Add drag handles to items, so they can be easily dragged into the inventory if characters etc...
        tree.EnumerateTree().Where(x => x.Value as ItemData).ForEach(AddDragHandles);
        tree.EnumerateTree().Where(x => x.Value as RecipeBaseSO).ForEach(AddDragHandles);
        tree.EnumerateTree().Where(x => x.Value as EntityBaseSO).ForEach(AddDragHandles);
        tree.EnumerateTree().Where(x => x.Value as TileSO).ForEach(AddDragHandles);

        // Add icons to characters and items.
        tree.EnumerateTree().AddIcons<ItemData>(x => x.icon);
        tree.EnumerateTree().AddIcons<RecipeBaseSO>(x => x.icon);
        tree.EnumerateTree().AddIcons<TileSO>(x => x.m_DefaultSprite);
        return tree;
    }
    protected override void OnImGUI() {
        // Add a button to resort the menu
        if (GUILayout.Button("Refresh")) {
            ForceMenuTreeRebuild();
        }

        // Draw the rest of the GUI
        base.OnImGUI();
    }

    private void AddSortedAssets<T>(OdinMenuTree tree, string category, string path) where T : ScriptableObject, IIdentifiable {
        var assets = LoadAndSortAssets<T>(path);
        foreach (var asset in assets) {
            // Use zero-padded ID for consistent sorting
            string menuPath = $"{category}/{asset.ID:D3} - {asset.name}";
            tree.Add(menuPath, asset);
        }
    }

    private List<T> LoadAndSortAssets<T>(string path) where T : ScriptableObject, IIdentifiable {
        var guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { path });
        var assets = guids
            .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(asset => asset != null)
            .ToList();
        assets.Sort((a, b) => a.ID.CompareTo(b.ID));
        return assets;
    }

    private void AddDragHandles(OdinMenuItem menuItem) {
        menuItem.OnDrawItem += x => DragAndDropUtilities.DragZone(menuItem.Rect, menuItem.Value, false, false);
    }

    
}