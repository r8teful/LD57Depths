using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.Text;
using Object = UnityEngine.Object;
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
        AddSortedAssets<UpgradeEffect>(tree, "UpgradeEffects", "Assets/Resources/UpgradeData");
        AddSortedAssets<RecipeBaseSO>(tree, "RecipiesUpgrade", "Assets/Resources/UpgradeData");
        AddSortedAssets<UpgradeNodeSO>(tree, "UpgradeNodes", "Assets/Resources/UpgradeNodeData");
        AddSortedAssets<EntityBaseSO>(tree, "Entities", "Assets/Resources/EntityData");
        AddSortedAssets<TileSO>(tree, "Tiles", "Assets/Resources/TileData");
        // Add drag handles to items, so they can be easily dragged into the inventory if characters etc...
        tree.EnumerateTree().Where(x => x.Value as ItemData).ForEach(AddDragHandles);
        tree.EnumerateTree().Where(x => x.Value as RecipeBaseSO).ForEach(AddDragHandles);
        tree.EnumerateTree().Where(x => x.Value as EntityBaseSO).ForEach(AddDragHandles);
        tree.EnumerateTree().Where(x => x.Value as TileSO).ForEach(AddDragHandles);
        tree.EnumerateTree().Where(x => x.Value as UpgradeNodeSO).ForEach(AddDragHandles);
        tree.EnumerateTree().Where(x => x.Value as UpgradeEffect).ForEach(AddDragHandles);
        // Double click for everything
        tree.EnumerateTree().Where(x => x.Value is Object).ForEach(AddDoubleClickSelect);    
        // Add icons to characters and items.
        tree.EnumerateTree().AddIcons<ItemData>(x => x.icon);
        tree.EnumerateTree().AddIcons<RecipeBaseSO>(x => x.icon);
        tree.EnumerateTree().AddIcons<UpgradeNodeSO>(x => x.icon);
        tree.EnumerateTree().AddIcons<TileSO>(x => x.m_DefaultSprite);
        return tree;
    }
    protected override void OnBeginDrawEditors() {
        if (MenuTree == null) {
            return;
        }

        // Use Odin's toolbar drawing helpers for a consistent look
        SirenixEditorGUI.BeginHorizontalToolbar(MenuTree.Config.SearchToolbarHeight);
        {
            // Refresh button 
            if (SirenixEditorGUI.ToolbarButton(new GUIContent("Refresh"), false)) {
                ForceMenuTreeRebuild();
            }
            // Rename
            if (SirenixEditorGUI.ToolbarButton(new GUIContent("Rename"), false)) {
                var selectedObj = MenuTree?.Selection?.SelectedValue as UnityEngine.Object ?? Selection.activeObject;
                if (selectedObj != null) {
                    string path = AssetDatabase.GetAssetPath(selectedObj);
                    if (!string.IsNullOrEmpty(path)) {
                        RenameAssetWindow.Open(path, (newName) => {
                            if (!string.IsNullOrEmpty(newName)) {
                                AssetDatabase.RenameAsset(path, newName);
                                AssetDatabase.SaveAssets();
                                //AssetDatabase.Refresh();

                                // reload the asset and try to select it
                                string newPath = AssetDatabase.GetAssetPath(selectedObj);
                                var newObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(newPath);
                                TrySelectMenuItemWithObject(newObj);
                                //Selection.activeObject = newObj;
                                //EditorGUIUtility.PingObject(newObj);
                                //ForceMenuTreeRebuild();
                            }
                        });
                    } else {
                        EditorUtility.DisplayDialog("Rename", "Selected object is not an on-disk asset.", "OK");
                    }
                } else {
                    EditorUtility.DisplayDialog("Rename", "Nothing selected to rename.", "OK");
                }
            }
            // New (create a new ScriptableObject next to the selected asset, or in Assets/)
            if (SirenixEditorGUI.ToolbarButton(new GUIContent("New"), false)) {
                var targetObj = MenuTree?.Selection?.SelectedValue as UnityEngine.Object ?? Selection.activeObject;
                var created = CreateNewAssetSameType(targetObj);
                if (created != null) {
                    //ForceMenuTreeRebuild();
                    TrySelectMenuItemWithObject(created);
                }
            }
            // Duplicate button
            if (SirenixEditorGUI.ToolbarButton(new GUIContent("Duplicate"), false)) {
                // Attempt to duplicate selected item from the menu first, fallback to Selection.activeObject
                var selectedObj = MenuTree?.Selection?.SelectedValue as UnityEngine.Object ?? Selection.activeObject;
                var newObj = DuplicateSelectedAsset(selectedObj);
                if (newObj != null) {
                    // Rebuild the tree and try to select the newly created asset in the menu
                    //ForceMenuTreeRebuild();
                    TrySelectMenuItemWithObject(newObj);
                }
            }
            
            // Delete
            if (SirenixEditorGUI.ToolbarButton(new GUIContent("Delete"), false)) {
                var selectedObj = MenuTree?.Selection?.SelectedValue as UnityEngine.Object ?? Selection.activeObject;
                if (selectedObj != null) {
                    string path = AssetDatabase.GetAssetPath(selectedObj);
                    if (!string.IsNullOrEmpty(path)) {
                        bool ok = EditorUtility.DisplayDialog(
                            "Delete Asset",
                            $"Are you sure you want to delete '{Path.GetFileName(path)}'?\nThis cannot be undone from this dialog.",
                            "Delete",
                            "Cancel"
                        );
                        if (ok) {
                            // Attempt delete
                            if (AssetDatabase.DeleteAsset(path)) {
                                AssetDatabase.SaveAssets();
                                AssetDatabase.Refresh();
                                Selection.activeObject = null;
                                ForceMenuTreeRebuild();
                            } else {
                                EditorUtility.DisplayDialog("Delete Failed", $"Failed to delete asset at '{path}'.", "OK");
                            }
                        }
                    } else {
                        EditorUtility.DisplayDialog("Delete", "Selected object is not an on-disk asset.", "OK");
                    }
                } else {
                    EditorUtility.DisplayDialog("Delete", "Nothing selected to delete.", "OK");
                }
            }
            // You can add extra toolbar buttons here if you want.
            GUILayout.FlexibleSpace();
        }
        SirenixEditorGUI.EndHorizontalToolbar();

        // Let base draw the rest of the editor UI (inspector pane etc.)
        base.OnBeginDrawEditors();
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
    private void AddDoubleClickSelect(OdinMenuItem menuItem) {
        // OnDrawItem is invoked while the menu item is drawn (you already used it for drag handles).
        menuItem.OnDrawItem += args =>
        {
            var evt = Event.current;
            // Check for mouse down with a double click inside the item's rect
            if (evt != null &&
                evt.type == EventType.MouseDown &&
                evt.clickCount == 2 &&
                menuItem.Rect.Contains(evt.mousePosition)) {
                // menuItem.Value is often the ScriptableObject (or other UnityEngine.Object)
                if (menuItem.Value is Object uobj) {
                    // Select & ping it in the Project window
                    Selection.activeObject = uobj;
                    EditorUtility.FocusProjectWindow();
                    EditorGUIUtility.PingObject(uobj);
                }

                // consume the event so other GUI doesn't also act on it
                evt.Use();
            }
        };
    }
    // Create a new ScriptableObject of the same type as 'reference' in the same folder (or 'Assets' if none)
    private Object CreateNewAssetSameType(Object reference) {
        Type createType = typeof(ScriptableObject);
        if (reference != null) {
            var refType = reference.GetType();
            if (typeof(ScriptableObject).IsAssignableFrom(refType))
                createType = refType;
        }

        string targetDirectory = "Assets";
        if (reference != null) {
            string refPath = AssetDatabase.GetAssetPath(reference);
            if (!string.IsNullOrEmpty(refPath)) {
                targetDirectory = Path.GetDirectoryName(refPath).Replace('\\', '/');
            }
        }

        string baseName = $"New {createType.Name}.asset";
        string destPath = Path.Combine(targetDirectory, baseName).Replace('\\', '/');
        destPath = AssetDatabase.GenerateUniqueAssetPath(destPath);

        var instance = ScriptableObject.CreateInstance(createType);
        AssetDatabase.CreateAsset(instance, destPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(destPath);
        AssetDatabase.Refresh();

        var created = AssetDatabase.LoadAssetAtPath<Object>(destPath);
        if (created != null) {
            Undo.RegisterCreatedObjectUndo(created, "Create Asset");
            Selection.activeObject = created;
            EditorGUIUtility.PingObject(created);
            return created;
        }

        return null;
    }
    // --- Duplicate helper (returns the duplicated asset object or null if failed) ---
    private Object DuplicateSelectedAsset(Object asset = null) {
        var obj = asset ?? Selection.activeObject;
        if (obj == null) {
            Debug.LogWarning("DuplicateSelectedAsset: No asset provided and nothing selected.");
            return null;
        }

        string sourcePath = AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(sourcePath)) {
            Debug.LogWarning($"DuplicateSelectedAsset: The object '{obj.name}' is not an asset on disk (path empty).");
            return null;
        }

        string directory = Path.GetDirectoryName(sourcePath).Replace('\\', '/');
        string filename = Path.GetFileName(sourcePath);
        string nameWithoutExt = Path.GetFileNameWithoutExtension(filename);
        string ext = Path.GetExtension(filename);

        // Unity-like base name for duplicate; GenerateUniqueAssetPath will handle numbering if needed
        string baseDuplicateName = $"{nameWithoutExt} Copy{ext}";
        string destPath = Path.Combine(directory, baseDuplicateName).Replace('\\', '/');

        destPath = AssetDatabase.GenerateUniqueAssetPath(destPath);

        if (AssetDatabase.CopyAsset(sourcePath, destPath)) {
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(destPath, ImportAssetOptions.Default);
            AssetDatabase.Refresh();

            var newObj = AssetDatabase.LoadAssetAtPath<Object>(destPath);
            if (newObj != null) {
                Selection.activeObject = newObj;
                EditorGUIUtility.PingObject(newObj);
                return newObj;
            }
        } else {
            Debug.LogError($"DuplicateSelectedAsset: Failed to copy asset from '{sourcePath}' to '{destPath}'.");
        }

        return null;
    }
}
/// <summary>
/// Small popup window to prompt for an asset name and call back when confirmed.
/// Uses AssetDatabase.RenameAsset(path, newName) to rename.
/// </summary>
public class RenameAssetWindow : EditorWindow {
    private string assetPath;
    private string currentNameWithoutExt;
    private Action<string> onRename;
    private string newName;

    public static void Open(string path, Action<string> onRename) {
        var win = CreateInstance<RenameAssetWindow>();
        win.assetPath = path;
        win.onRename = onRename;

        string filename = Path.GetFileNameWithoutExtension(path);
        win.currentNameWithoutExt = filename;
        win.newName = filename;

        win.titleContent = new GUIContent("Rename Asset");
        win.position = new Rect(Screen.width / 2f, Screen.height / 2f, 420, 86);
        win.ShowUtility();
    }

    private void OnGUI() {
        EditorGUILayout.LabelField("Rename asset:", EditorStyles.boldLabel);
        newName = EditorGUILayout.TextField(newName);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("OK")) {
            if (!string.IsNullOrWhiteSpace(newName)) {
                onRename?.Invoke(newName);
            }
            Close();
        }
        if (GUILayout.Button("Cancel")) {
            Close();
        }
        EditorGUILayout.EndHorizontal();
    }
}