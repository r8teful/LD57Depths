using Sirenix.OdinInspector;
using Sirenix.Utilities;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

[CreateAssetMenu(fileName = "WorldSpawnEntitySO", menuName = "ScriptableObjects/WorldSpawnEntitySO", order = 4)]
// An entity that is predetermined to spawn from within the world generation
public class WorldSpawnEntitySO : EntityBaseSO {
    [Header("Placement Rules")]
    public float placementFrequency = 0.1f; // Noise frequency for density/clustering control
    [Range(0f, 1f)] public float placementThreshold = 0.7f; // Noise value needed at anchor point
    public List<AttachmentType> allowedAttachmentTypes;
    [Header("Spawn Conditions at Anchor Point")]
    public bool requireSolidGround = true; // Must the anchor tile be 'rock'?
    public bool requireWaterAdjacent = false;   // Must be next to MainWater or CaveWater?

    [OnValueChanged("Test")]
    [TableMatrix(DrawElementMethod = "DrawColoredEnumElement", ResizableColumns = false,
        SquareCells =true,HideColumnIndices =false,HideRowIndices =false)]
    public bool[,] areaMatrix = new bool[9, 9];
#if UNITY_EDITOR
    private static bool DrawColoredEnumElement(Rect rect, bool value) {
        if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition)) {
            value = !value;
            GUI.changed = true;
            Event.current.Use();
        }
        //UnityEditor.EditorGUI.DrawRect(rect.Padding(1), value ? new Color(0.1f, 0.8f, 0.2f) : new Color(0, 0, 0, 0.5f));
        UnityEditor.EditorGUI.DrawRect(rect.Padding(1), value ? new Color(0.8f, 0.2f, 0.7f) : new Color(0, 0, 0, 0.5f));

        return value;
    }

    [OnInspectorInit]
    private void CreateData() {
        //areaMatrix = new bool[9,9];
        //leftRightBounding = LeftRightBounding();
    }
    private void Test() {
        GetBoundingOffset();
    }
#endif
    [Header("Placement Fine-tuning")]
    public bool randomYRotation = true;
    public Vector2 scaleVariation = Vector2.one; // Min/Max uniform scale multiplier
    
    // This gets the offset bounds from the middle bottom tile (4,8)
    public (Vector2Int, Vector2Int) GetBoundingOffset() {
        int minRow = int.MaxValue;
        int maxRow = int.MinValue;
        int minCol = int.MaxValue;
        int maxCol = int.MinValue;
        for (int r = 0; r < 9; r++) {
            for (int c = 0; c < 9; c++) {
                if (areaMatrix[r, c]) {
                    if (r < minRow)
                        minRow = r;
                    if (r > maxRow)
                        maxRow = r;
                    if (c < minCol)
                        minCol = c;
                    if (c > maxCol)
                        maxCol = c;
                }
            }
        }
        var (minRowOffset, minColOffset, maxRowOffset, maxColOffset) = RemapBounds(minRow,minCol,maxRow,maxCol);

        //Debug.Log($"OLD {minRow}, {minCol} AND {maxRow}, {maxCol}");
        Debug.Log($"{minRowOffset}, {minColOffset} AND {maxRowOffset}, {maxColOffset}");
        return (new(minRowOffset, minColOffset), new(maxRowOffset, maxColOffset));
    }
    // Remaps a single (row, col) point.
    private static (int row, int col) RemapPoint(int row, int col) {
        int newRow = row - 4;
        int newCol = -col + 8;
        return (newRow, newCol);
    }

    /// <summary>
    /// Given an axis-aligned bounding box specified by
    /// (minRow, minCol) to (maxRow, maxCol),
    /// remap it so that (0,0)->(9,-4) and (8,8)->(1,4).
    /// </summary>
    public static (int newMinRow, int newMinCol, int newMaxRow, int newMaxCol)
        RemapBounds(int minRow, int minCol, int maxRow, int maxCol) {
        // Map both corners:
        var (r1, c1) = RemapPoint(minRow, minCol);
        var (r2, c2) = RemapPoint(maxRow, maxCol);

        int newMinRow = Mathf.Min(r1, r2);
        int newMaxRow = Mathf.Max(r1, r2);

        int newMinCol = Mathf.Min(c1, c2);
        int newMaxCol = Mathf.Max(c1, c2);

        return (newMinRow, newMinCol, newMaxRow, newMaxCol);
    }
}

public enum AttachmentType {
    None,
    Ground, Ceiling, WallRight, WallLeft
}