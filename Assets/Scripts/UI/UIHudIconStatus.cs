using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIHudIconStatus: UIHudIconBase{
    [SerializeField] private TextMeshProUGUI timeText;
    public void Init(Sprite icon, string description) {
        InitBase(icon, "",description);
    }
    public void SetTime(float t) {
        timeText.text = FormatSeconds(t);
    }
    string FormatSeconds(float sec) {
        if (sec <= 0) return "0s";
        int s = Mathf.CeilToInt(sec);
        if (s >= 60) return $"{s / 60}m {s % 60}s";
        return $"{s}s";
    }
}