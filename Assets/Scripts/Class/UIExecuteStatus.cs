
[System.Serializable]
public abstract class UIExecuteStatus { }
public class StatChangeStatus : UIExecuteStatus {
    public string StatName { get; }
    public string ValueNow { get; }
    public string ValueNext { get; }
    public bool IsBadChange { get; }
    public StatChangeStatus(string statName, float valueNow, float valueNext, bool isBadChange) {
        StatName = statName;
        ValueNow = valueNow.ToString("F2");
        ValueNext = valueNext.ToString("F2");
        IsBadChange = isBadChange;
    }
    public StatChangeStatus(string statName, string valueNow, string valueNext, bool isBadChange) {
        StatName = statName;
        ValueNow = valueNow;
        ValueNext = valueNext;
        IsBadChange = isBadChange;
    }
}
public class ItemGainStatus : UIExecuteStatus {
    public ItemGainStatus(ItemQuantity itemQuantity) {
        ItemQuantity = itemQuantity;
    }

    public ItemQuantity ItemQuantity { get; }
}