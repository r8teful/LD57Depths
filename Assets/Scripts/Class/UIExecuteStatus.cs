
[System.Serializable]
public abstract class UIExecuteStatus { }
public class StatChangeStatus : UIExecuteStatus {
    public string StatName { get; set; }
    public string ValueNow { get; set; }
    public string ValueNext { get; set; }
    public bool IsBadChange { get; set; }
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