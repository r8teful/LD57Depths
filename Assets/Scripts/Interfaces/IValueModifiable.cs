public interface IValueModifiable {
    void ModifyValue(ValueModifier modifier);
    void Register();
    float GetValueNow(ValueKey key);
    float GetValueBase(ValueKey key);
    void ReturnValuesToBase(); // Just so I can call ClearAllUpgrades and it clears all the things lollol
}
public enum ValueKey {
    MagnetismPickup = 10,
    MagnetismStrength = 11,
    ItemTransferRate = 20,
    LazerChainAmount = 30,
    LazerChainDamage = 31 // as mult
}