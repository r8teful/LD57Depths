using System;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "InputIconDatabase", menuName = "ScriptableObjects/Other/InputIconDatabase")]
public class TextIconsDatabase : ScriptableObject {

    [Tooltip("Name of the TMP Sprite Atlas asset (as configured in TMP Settings).")]
    public string atlasName = "Keyboard";

    [Tooltip("Fallback sprite name when a binding path has no matching entry.")]
    public string fallbackSpriteName = "unknown_key";

    [Tooltip("Each entry maps one binding path to a sprite name in the atlas.")]
    public List<IconEntry> entries = new();

    // ── Runtime lookup (built on first query)
    private Dictionary<string, string> _lookup;

    /// <summary>
    /// Returns the sprite name for a given binding path (e.g. "&lt;Keyboard&gt;/space"),
    /// or the fallback sprite name if no entry is found.
    /// </summary>
    public string GetSpriteName(string bindingPath) {
        BuildLookupIfNeeded();

        // Try exact match first
        if (_lookup.TryGetValue(bindingPath, out string spriteName))
            return spriteName;

        // Try case-insensitive match as a fallback
        foreach (var kvp in _lookup) {
            if (string.Equals(kvp.Key, bindingPath, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }

        return fallbackSpriteName;
    }

    /// <summary>
    /// Builds the TMP rich text sprite tag for a given binding path.
    /// Result: <sprite atlas="Keyboard" name="space">
    /// </summary>
    public string GetSpriteTag(string bindingPath) {
        string spriteName = GetSpriteName(bindingPath);
        return $"<sprite atlas=\"{atlasName}\" name=\"{spriteName}\">";
    }


    public void InvalidateLookup() => _lookup = null;

    private void BuildLookupIfNeeded() {
        if (_lookup != null)
            return;

        _lookup = new Dictionary<string, string>(entries.Count, StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries) {
            if (!string.IsNullOrWhiteSpace(entry.bindingPath))
                _lookup[entry.bindingPath] = entry.spriteName;
        }
    }

    [Serializable]
    public struct IconEntry {
        [Tooltip("Full Input System binding path, e.g. '<Keyboard>/space' or '<Gamepad>/buttonSouth'")]
        public string bindingPath;

        [Tooltip("Name of the sprite in the TMP Sprite Atlas, e.g. 'space_key'")]
        public string spriteName;
    }
}