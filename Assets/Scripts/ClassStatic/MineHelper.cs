using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
public static class MineHelper {
    // A simple struct to hold our results
    public struct TileDamageData {
        public Vector3Int CellPos;
        public float DamageRatio; // 0.0 to 1.0

        public TileDamageData(Vector3Int cell, float ratio) {
            CellPos = cell;
            DamageRatio = ratio;
        }
    }
    public static List<TileDamageData> GetCircle(Tilemap map, Vector3 position, float radius,bool useFalloff = false) {
        List<TileDamageData> result = new List<TileDamageData>();
        int gridRadius = Mathf.CeilToInt(radius);
        Vector3Int centerCell = map.WorldToCell(position);
        for (int x = -gridRadius; x <= gridRadius; x++) {
            for (int y = -gridRadius; y <= gridRadius; y++) {
                Vector3Int tilePos = centerCell + new Vector3Int(x, y, 0);
                Vector3 tileWorldPos = map.GetCellCenterWorld(tilePos); // Use center of the tile for distance checks
                float dist = Vector2.Distance(position, tileWorldPos);
                if (dist <= radius) {
                    float ratio = useFalloff ? Mathf.Clamp01(1f - (dist / radius)) : 1f;
                    result.Add(new TileDamageData(tilePos, ratio));
                }
            }
        }
        return result;
    }
  
    public static List<TileDamageData> GetCone(Tilemap map, Vector2 origin, Vector2 direction, float length, float halfAngle, bool useFalloff = true) {
        var result = new List<TileDamageData>();
        int gridLen = Mathf.CeilToInt(length);
        Vector3Int centerCell = map.WorldToCell(origin);

        // Simple loop around the origin based on length
        for (int x = -gridLen; x <= gridLen; x++) {
            for (int y = -gridLen; y <= gridLen; y++) {
                Vector3Int tilePos = centerCell + new Vector3Int(x, y, 0);
                Vector3 tileWorldPos = map.GetCellCenterWorld(tilePos);

                Vector2 toTile = (Vector2)tileWorldPos - origin;
                float dist = toTile.magnitude;

                if (dist > length) continue; // Too far
                if (dist <= 0.01f) continue; // Don't hit self

                if (Vector2.Angle(direction, toTile) <= halfAngle) {
                    float ratio = useFalloff ? Mathf.Clamp01(1f - (dist / length)) : 1f;
                    result.Add(new TileDamageData(tilePos, ratio));
                }
            }
        }
        return result;
    }

    public static List<TileDamageData> GetRect(Tilemap map, Vector2 origin, Vector2 direction, float width, float length, bool useFalloff = true) {
        var result = new List<TileDamageData>();
        int reach = Mathf.CeilToInt(length); // How far to loop
        Vector3Int centerCell = map.WorldToCell(origin);

        // We need the "Right" vector to check width
        Vector2 right = new Vector2(-direction.y, direction.x);

        for (int x = -reach; x <= reach; x++) {
            for (int y = -reach; y <= reach; y++) {
                Vector3Int tilePos = centerCell + new Vector3Int(x, y, 0);
                Vector3 tileWorldPos = map.GetCellCenterWorld(tilePos);
                Vector2 offset = (Vector2)tileWorldPos - origin;

                // Math trick: Projection (Dot Product)
                // "How far forward are we?"
                float forwardDist = Vector2.Dot(offset, direction);
                // "How far sideways are we?"
                float sideDist = Mathf.Abs(Vector2.Dot(offset, right));

                // Check if we are inside the box
                if (forwardDist >= 0 && forwardDist <= length && sideDist <= width / 2) {
                    float ratio = useFalloff ? Mathf.Clamp01(1f - (forwardDist / length)) : 1f;
                    result.Add(new TileDamageData(tilePos, ratio));
                }
            }
        }
        return result;
    }

    public static TileDamageData? GetClosestSolidTile(Tilemap map, Vector3 position, float radius, List<Vector3Int> pointsToExclude) {
        int gridRadius = Mathf.CeilToInt(radius);
        Vector3Int centerCell = map.WorldToCell(position);
        float radiusSqr = radius * radius;

        TileDamageData? closestResult = null;
        float closestDistSqr = float.MaxValue;

        for (int x = -gridRadius; x <= gridRadius; x++) {
            for (int y = -gridRadius; y <= gridRadius; y++) {
                Vector3Int tilePos = centerCell + new Vector3Int(x, y, 0);
                if (pointsToExclude.Contains(tilePos)) continue;
                if (!map.HasTile(tilePos)) continue;

                Vector3 tileWorldPos = map.GetCellCenterWorld(tilePos);
                float distSqr = (position - tileWorldPos).sqrMagnitude;

                // Only check logic if it is within radius AND closer than what we found so far
                if (distSqr <= radiusSqr && distSqr < closestDistSqr) {
                    var tileData = map.GetTile<TileSO>(tilePos);

                    if (tileData != null && tileData.IsSolid) {
                        closestDistSqr = distSqr;
                        closestResult = new TileDamageData(tilePos, 1f);
                    }
                }
            }
        }

        return closestResult;
    }
    public static List<TileDamageData> GetLightningBolt(
    Tilemap map,
    Vector3 startPosition,
    Vector3 baseDirection,
    int steps,
    float stepLength,
    float deviationAngle = 45f,
    bool checkSolid = false,
    bool useFalloff = false) {
        List<TileDamageData> result = new List<TileDamageData>();

        // Track visited cells to avoid duplicate entries
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();
        baseDirection.Normalize();

        Vector3 currentPos = startPosition;

        for (int i = 0; i < steps; i++) {
            // Deviate from the base direction by a random angle each step
            float randomAngle = Random.Range(-deviationAngle, deviationAngle);
            Vector3 stepDirection = Quaternion.Euler(0f, 0f, randomAngle) * baseDirection;
            stepDirection.Normalize();

            Vector3 nextPos = currentPos + stepDirection * stepLength;

            // Damage ratio: 1.0 at start, falls toward 0.0 at end if falloff is enabled
            float ratio = useFalloff ? Mathf.Clamp01(1f - ((float)i / steps)) : 1f;

            // Collect all cells touched on the line between current and next position
            List<Vector3Int> cellsOnSegment = GetCellsOnLine(map, currentPos, nextPos);

            foreach (Vector3Int cell in cellsOnSegment) {
                if (visited.Contains(cell))
                    continue;
                if (checkSolid && !IsSolid(map, cell))
                    continue;

                visited.Add(cell);
                result.Add(new TileDamageData(cell, ratio));
            }

            currentPos = nextPos;
        }

        return result;
    }

    private static bool IsSolid(Tilemap map, Vector3Int cell) {
        return true; // TODO 
    }

    /// <summary>
    /// Returns all tilemap cells intersected by a line segment between two world positions,
    /// using Bresenham's line algorithm on cell coordinates.
    /// </summary>
    private static List<Vector3Int> GetCellsOnLine(Tilemap map, Vector3 from, Vector3 to) {
        List<Vector3Int> cells = new List<Vector3Int>();

        Vector3Int fromCell = map.WorldToCell(from);
        Vector3Int toCell = map.WorldToCell(to);

        int x0 = fromCell.x, y0 = fromCell.y;
        int x1 = toCell.x, y1 = toCell.y;

        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = (x0 < x1) ? 1 : -1;
        int sy = (y0 < y1) ? 1 : -1;
        int err = dx - dy;

        while (true) {
            cells.Add(new Vector3Int(x0, y0, 0));

            if (x0 == x1 && y0 == y1)
                break;

            int e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }

        return cells;
    }
}