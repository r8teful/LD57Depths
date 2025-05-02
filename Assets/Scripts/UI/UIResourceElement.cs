using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIResourceElement : MonoBehaviour {
    public Image resourceIcon;
    public TextMeshProUGUI resourceAmountText;
    public TileScript.TileType ResourceType;
    public void Init(TileScript.TileType type, int amount) {
        ResourceType = type;
        Sprite sprite = null;
        switch (type) {
            case TileScript.TileType.Empty:
                break;
            case TileScript.TileType.Dirt:
                break;
            case TileScript.TileType.Ore_Stone:
                sprite = Resources.Load<Sprite>("Ores/UIStone");
                break;
            case TileScript.TileType.Ore_Ruby:
                sprite = Resources.Load<Sprite>("Ores/UIRuby");
                break;
            case TileScript.TileType.Ore_Silver:
                sprite = Resources.Load<Sprite>("Ores/UISilver");
                break;
            case TileScript.TileType.Boundary:
                break;
            case TileScript.TileType.Ore_Gold:
                sprite = Resources.Load<Sprite>("Ores/UIGold");
                break;
            case TileScript.TileType.Ore_Diamond:
                sprite = Resources.Load<Sprite>("Ores/UIDiamond");
                break;
            default:
                break;
        }
        if (sprite != null) {
            resourceIcon.sprite = sprite;
        }
        resourceAmountText.text = amount.ToString();
    }
}
