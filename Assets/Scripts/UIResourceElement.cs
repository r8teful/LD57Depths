using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIResourceElement : MonoBehaviour {
    public Image resourceIcon;
    public TextMeshProUGUI resourceAmountText;
    public Tile.TileType ResourceType;
    public void Init(Tile.TileType type, int amount) {
        ResourceType = type;
        Sprite sprite = null;
        switch (type) {
            case Tile.TileType.Empty:
                break;
            case Tile.TileType.Dirt:
                break;
            case Tile.TileType.Ore_Stone:
                sprite = Resources.Load<Sprite>("Ores/UIStone");
                break;
            case Tile.TileType.Ore_Ruby:
                sprite = Resources.Load<Sprite>("Ores/UIRuby");
                break;
            case Tile.TileType.Ore_Silver:
                sprite = Resources.Load<Sprite>("Ores/UISilver");
                break;
            case Tile.TileType.Boundary:
                break;
            case Tile.TileType.Ore_Gold:
                sprite = Resources.Load<Sprite>("Ores/UIGold");
                break;
            case Tile.TileType.Ore_Diamond:
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
