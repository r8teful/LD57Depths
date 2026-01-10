using TMPro;
using UnityEngine;

public class UIStatDisplayElement : MonoBehaviour {
    [SerializeField] TextMeshProUGUI statText;
    [SerializeField] TextMeshProUGUI statValueText;

    public void Init(StatType stat,float valueEffective, float valueRaw = 0) {
        //statText.text = stat.ToString();
        statText.text = ResourceSystem.GetStatString(stat);
        if(valueRaw == 0) {
            statValueText.text = $"{valueEffective:F2}";
        }else {
            statValueText.text = $"{valueEffective:F2}({valueRaw:F2})";

        }
    }
}
