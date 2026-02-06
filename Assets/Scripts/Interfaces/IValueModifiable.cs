using System.Collections;
using UnityEngine;

public interface IValueModifiable {
    void ModifyValue(ValueModifier modifier);
    void Register();
    float GetValue(ValueKey key);
}
public enum ValueKey {
    MagnetismPickup = 10,
    MagnetismStrength = 11
}