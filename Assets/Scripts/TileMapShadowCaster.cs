using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Generates ShadowCaster2Ds for every continuous block of a tilemap on Awake, applying some settings.
/// </summary>
public class TilemapShadowCaster : MonoBehaviour {
    [SerializeField]
    protected CompositeCollider2D m_TilemapCollider;

    [SerializeField]
    protected bool m_SelfShadows = true;

    protected virtual void Reset() {
        m_TilemapCollider = GetComponent<CompositeCollider2D>();
    }

    protected virtual void Awake() {
        GenerateTilemapShadowCasters(m_TilemapCollider, m_SelfShadows);
    }
    /// <summary>
    /// Given a Composite Collider 2D, it replaces existing Shadow Caster 2Ds (children) with new Shadow Caster 2D objects whose
    /// shapes coincide with the paths of the collider.
    /// </summary>
    /// <remarks>
    /// It is recommended that the object that contains the collider component has a Composite Shadow Caster 2D too.
    /// It is recommended to call this method in editor only.
    /// </remarks>
    /// <param name="collider">The collider which will be the parent of the new shadow casters.</param>
    /// <param name="selfShadows">Whether the shadow casters will have the Self Shadows option enabled..</param>

    [Button("CareTileMapShadows")]
    public void GenerateTilemapShadowCasters(CompositeCollider2D collider, bool selfShadows) {
        // First, it destroys the existing shadow casters
        ShadowCaster2D[] existingShadowCasters = collider.GetComponentsInChildren<ShadowCaster2D>();
        if(existingShadowCasters != null) {
            for (int i = 0; i < existingShadowCasters.Length; ++i) {
                if (existingShadowCasters[i].transform.parent != collider.transform) {
                    continue;
                }

                GameObject.DestroyImmediate(existingShadowCasters[i].gameObject);
            }
        }

        // Then it creates the new shadow casters, based on the paths of the composite collider
        int pathCount = collider.pathCount;
        List<Vector2> pointsInPath = new List<Vector2>();
        List<Vector3> pointsInPath3D = new List<Vector3>();

        for (int i = 0; i < pathCount; ++i) {
            collider.GetPath(i, pointsInPath);

            GameObject newShadowCaster = new GameObject("ShadowCaster2D");
            newShadowCaster.isStatic = true;
            newShadowCaster.transform.SetParent(collider.transform, false);

            for (int j = 0; j < pointsInPath.Count; ++j) {
                pointsInPath3D.Add(pointsInPath[j]);
            }

            ShadowCaster2D component = newShadowCaster.AddComponent<ShadowCaster2D>();
            component.SetPath(pointsInPath3D.ToArray());
            component.SetPathHash(Random.Range(int.MinValue, int.MaxValue)); // The hashing function GetShapePathHash could be copied from the LightUtility class
            component.selfShadows = selfShadows;
            component.Update();

            pointsInPath.Clear();
            pointsInPath3D.Clear();
        }
    }
}