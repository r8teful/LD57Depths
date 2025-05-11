
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// It extends the ShadowCaster2D class in order to be able to modify some private data members.
/// </summary>
public static class ShadowCaster2DExtensions {

    private static FieldInfo _shapePathField;
    private static FieldInfo _shapePathHashField;

    static ShadowCaster2DExtensions() // Static constructor for one-time initialization
    {
        _shapePathField = typeof(ShadowCaster2D).GetField("m_ShapePath",
                                                          BindingFlags.NonPublic |
                                                          BindingFlags.Instance);
        if (_shapePathField == null) {
            Debug.LogError("ShadowCaster2DExtensions: Failed to find m_ShapePath field. This might break shadow casting. Unity API may have changed.");
        }

        _shapePathHashField = typeof(ShadowCaster2D).GetField("m_ShapePathHash",
                                                               BindingFlags.NonPublic |
                                                               BindingFlags.Instance);
        if (_shapePathHashField == null) {
            Debug.LogError("ShadowCaster2DExtensions: Failed to find m_ShapePathHash field. This might break shadow casting. Unity API may have changed.");
        }
    }

    public static void SetPathOptimized(this ShadowCaster2D shadowCaster, Vector3[] path) {
        if (_shapePathField != null) {
            _shapePathField.SetValue(shadowCaster, path);
        } else {
            Debug.LogError("ShadowCaster2DExtensions: m_ShapePath field is not available. Cannot set path.");
        }
    }

    public static void SetPathHashOptimized(this ShadowCaster2D shadowCaster, int hash) {
        if (_shapePathHashField != null) {
            _shapePathHashField.SetValue(shadowCaster, hash);
        } else {
            Debug.LogError("ShadowCaster2DExtensions: m_ShapePathHash field is not available. Cannot set path hash.");
        }
    }
    /// <summary>
    /// Replaces the path that defines the shape of the shadow caster.
    /// </summary>
    /// <remarks>
    /// Calling this method will change the shape but not the mesh of the shadow caster. Call SetPathHash afterwards.
    /// </remarks>
    /// <param name="shadowCaster">The object to modify.</param>
    /// <param name="path">The new path to define the shape of the shadow caster.</param>
    public static void SetPath(this ShadowCaster2D shadowCaster, Vector3[] path) {
        FieldInfo shapeField = typeof(ShadowCaster2D).GetField("m_ShapePath",
                                                               BindingFlags.NonPublic |
                                                               BindingFlags.Instance);
        shapeField.SetValue(shadowCaster, path);
    }

    /// <summary>
    /// Replaces the hash key of the shadow caster, which produces an internal data rebuild.
    /// </summary>
    /// <remarks>
    /// A change in the shape of the shadow caster will not block the light, it has to be rebuilt using this function.
    /// </remarks>
    /// <param name="shadowCaster">The object to modify.</param>
    /// <param name="hash">The new hash key to store. It must be different from the previous key to produce the rebuild. You can use a random number.</param>
    public static void SetPathHash(this ShadowCaster2D shadowCaster, int hash) {
        FieldInfo hashField = typeof(ShadowCaster2D).GetField("m_ShapePathHash",
                                                              BindingFlags.NonPublic |
                                                              BindingFlags.Instance);
        hashField.SetValue(shadowCaster, hash);
    }
}