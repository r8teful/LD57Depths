using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIResourceElement : MonoBehaviour {
    public Image resourceIcon;
    public TextMeshProUGUI resourceAmountText;

    public void Init(Tile.TileType type, int amount) {
        Sprite sprite = null;
        switch (type) {
            case Tile.TileType.Empty:
                break;
            case Tile.TileType.Dirt:
                break;
            case Tile.TileType.Ore_Stone:
                sprite = Resources.Load<Sprite>("UI/IconStone");
                break;
            case Tile.TileType.Ore_Ruby:
                sprite = Resources.Load<Sprite>("UI/IconCopper");
                break;
            case Tile.TileType.Ore_Silver:
                sprite = Resources.Load<Sprite>("UI/IconSilver");
                break;
            case Tile.TileType.Boundary:
                break;
            default:
                break;
        }
        if(sprite != null) {
            resourceIcon.sprite = sprite;
        }
        resourceAmountText.text = amount.ToString();
    }
}
