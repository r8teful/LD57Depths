public interface IValueModifiable {
    void ModifyValue(ValueModifier modifier);
    void Register();
    float GetValueNow(ValueKey key);
    float GetValueBase(ValueKey key);
}
public enum ValueKey {
    MagnetismPickup = 10,
    MagnetismStrength = 11,
    ItemTransferRate = 20,
    LazerChainAmount = 30,
    LazerChainDamage = 31 // as mult
}