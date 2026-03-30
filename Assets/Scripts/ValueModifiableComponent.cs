using System;
using System.Collections.Generic;
using UnityEngine;
// ValueEntry.cs
[System.Serializable]
public struct ValueEntry {
    public ValueKey Key;
    public float BaseValue;
}
public class ValueModifiableComponent : MonoBehaviour {
    [SerializeField] private List<ValueEntry> _entries = new();

    private readonly Dictionary<ValueKey, float> _baseValues = new();
    private readonly Dictionary<ValueKey, float> _currentValues = new();

    public event Action<ValueKey, float> OnValueChanged;

    private void Awake() {
        foreach (var entry in _entries) {
            _baseValues[entry.Key] = entry.BaseValue;
            _currentValues[entry.Key] = entry.BaseValue;
        }
    }

    public void Register() {
        foreach (var key in _baseValues.Keys)
            PlayerManager.Instance.UpgradeManager.RegisterValueModifierScript(key, this);
    }

    public float GetValueBase(ValueKey key) =>
        _baseValues.TryGetValue(key, out var v) ? v : 1f;

    public float GetValueNow(ValueKey key) =>
        _currentValues.TryGetValue(key, out var v) ? v : 1f;

    public void ModifyValue(ValueModifier modifier) {
        if (_currentValues.ContainsKey(modifier.Key)) {
            _currentValues[modifier.Key] = UpgradeCalculator.CalculateNewUpgradeValue(
                _currentValues[modifier.Key], modifier);
            OnValueChanged?.Invoke(modifier.Key, _currentValues[modifier.Key]);
        }

    }

    public void ReturnValuesToBase() {
        foreach (var key in _baseValues.Keys)
            _currentValues[key] = _baseValues[key];
    }
}
public enum ValueKey {
    GravestoneHoldProcent = 5, // as mult
    MagnetismPickup = 10,
    MagnetismStrength = 11,
    ItemTransferRate = 20,
    LazerChainLength = 30,
    LazerChainDamage = 31, // as mult
    LazerChainChance = 32,
    ExplosiveCritChance = 40,
    ExplosiveCritDamage= 41,
    ExplosiveCritRange= 42,
    BlockOxygenChance = 50,
    BlockOxygenAmount = 51
}