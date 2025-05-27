using System.Collections.Generic;

public interface IPopupInfo {
    PopupData GetPopupData();
}

[System.Serializable]
public class PopupData {
    public string title;
    public string description;
    public List<RequiredItem> craftingInfo;

    public PopupData(string title, string description, List<RequiredItem> craftingInfo) {
        this.title = title;
        this.description = description;
        this.craftingInfo = craftingInfo;
    }
}