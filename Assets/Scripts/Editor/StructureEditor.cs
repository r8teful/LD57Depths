using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

[CustomEditor(typeof(StructureSO))]
public class StructureEditor : Editor {
    public override void OnInspectorGUI() {
        // Draw the default UI
        DrawDefaultInspector();
        StructureSO template = (StructureSO)target;
        GUILayout.Space(10);
        if (GUILayout.Button("Bake Structure from Prefab", GUILayout.Height(40))) {
            BakeStructure(template);
        }

        // Show a little info box
        if (template.tiles != null && template.tiles.Count > 0) {
            EditorGUILayout.HelpBox($"Baked: {template.Size.x}x{template.Size.y} ({template.tiles.Count} tiles)", MessageType.Info);
        }
    }

    private void BakeStructure(StructureSO template) {
        if (template.sourcePrefab == null) {
            Debug.LogError("Please assign a Source Prefab containing a Grid/Tilemap.");
            return;
        }

        // Identify the Tilemap inside the prefab
        Tilemap tilemap = template.sourcePrefab.GetComponentInChildren<Tilemap>();
        if (tilemap == null) {
            Debug.LogError("The source prefab does not contain a Tilemap component.");
            return;
        }

        // Compress bounds ensures the box fits exactly around the drawn tiles
        tilemap.CompressBounds();
        BoundsInt bounds = tilemap.cellBounds;

        template.Size = new Vector2Int(bounds.size.x, bounds.size.y);
        template.tiles = new List<TileBase>();

        // Loop Bottom-Left to Top-Right (matching your spawn logic)
        for (int y = bounds.yMin; y < bounds.yMax; y++) {
            for (int x = bounds.xMin; x < bounds.xMax; x++) {
                Vector3Int pos = new Vector3Int(x, y, 0);
                TileBase tile = tilemap.GetTile(pos);

                // Add to list (null is allowed for empty air)
                template.tiles.Add(tile);
            }
        }

        EditorUtility.SetDirty(template); // Mark SO as changed so Unity saves it
        Debug.Log($"<color=green>Structure '{template.name}' baked successfully!</color> Size: {template.Size}");
    }
}