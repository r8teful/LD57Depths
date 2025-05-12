using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
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

        // Add all scriptable object items.
        tree.AddAllAssetsAtPath("Items", "Assets/Resources/ItemData", typeof(ItemData), true);
        tree.AddAllAssetsAtPath("Entities", "Assets/Resources/EntityData", typeof(EntityBaseSO), true);
        tree.AddAllAssetsAtPath("Tiles", "Assets/Resources/TileData", typeof(TileSO), true);
        // Now set up each item's icon
        foreach (var item in tree.MenuItems) {
            // Value is the SO asset
            if (item.Value is EntityBaseSO entitySo) {
                // Assume your SO has a Sprite field called "previewSprite"
                // (or you can fetch from a prefab referenced inside it)
                Sprite spr = entitySo.entityPrefab.GetComponentInChildren<SpriteRenderer>().sprite;
                if (spr != null) {
                    // Use the sprite's texture as the menu icon
                    item.Icon = spr.texture;
                }
            }
        }

        // Finally, select the first item so you get to see the inspector on open
      //  tree.Selection.SelectIndex(0);
        // Add icons to characters and items.
        //tree.EnumerateTree().AddIcons<Character>(x => x.Icon);
        //tree.EnumerateTree().AddIcons<Item>(x => x.Icon);
        return tree;
    }
   
    private void AddDragHandles(OdinMenuItem menuItem) {
        menuItem.OnDrawItem += x => DragAndDropUtilities.DragZone(menuItem.Rect, menuItem.Value, false, false);
    }

    
}