using TMPro;
using UnityEngine;

public class UIStatDisplayElement : MonoBehaviour {
    [SerializeField] TextMeshProUGUI statText;
    [SerializeField] TextMeshProUGUI statValueText;

    public void Init(StatType stat, float value) {
        statText.text = stat.ToString();
        statValueText.text = value.ToString("F2");
    }
}
