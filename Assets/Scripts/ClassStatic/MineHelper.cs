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

    public static TileDamageData? GetClosestSolidTile(Tilemap map, Vector3 position, float radius, Vector3Int exlude) {
        int gridRadius = Mathf.CeilToInt(radius);
        Vector3Int centerCell = map.WorldToCell(position);
        float radiusSqr = radius * radius;

        TileDamageData? closestResult = null;
        float closestDistSqr = float.MaxValue;

        for (int x = -gridRadius; x <= gridRadius; x++) {
            for (int y = -gridRadius; y <= gridRadius; y++) {
                Vector3Int tilePos = centerCell + new Vector3Int(x, y, 0);
                if (tilePos == exlude) continue;
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
}