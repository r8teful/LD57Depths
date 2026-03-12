using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Changes the colors of a button and its text based on the state (ButtonColor)
public class ButtonMenuVisual: MonoBehaviour {
    [SerializeField] private Image _image;
    [SerializeField] private TextMeshProUGUI _text;

    [OnValueChanged("InspectorColorChange")]
    public ButtonColor DefaultColor;
    [OnValueChanged("InspectorSizeChange")]
    public ButtonSize Size;
    [Header("Visual")]
    [SerializeField] private Sprite SpriteDissabled;
    [SerializeField] private Color ColorDissabled;
    [SerializeField] private Sprite SpriteOrange;
    [SerializeField] private Color ColorOrange;
    [SerializeField] private Sprite SpriteGreen;
    [SerializeField] private Color ColorGreen;
    private void OnValidate() {
        ChangeStateSize(Size);
        ChangeStateColor(DefaultColor);
    }
    public void InspectorColorChange() {
        ChangeStateColor(DefaultColor);
    }
    public void InspectorSizeChange() {
        ChangeStateSize(Size);
    }

    public enum ButtonColor {
        Dissabled,
        Green,
        Orange
    }
    public enum ButtonSize {
        Small,
        Medium,
        Large
    }
    private void Awake() {
        if(_image == null ||  _text == null) {
            Debug.LogError("Coudn't find valid components!");
        }
    }
    public void ChangeStateColor(ButtonColor c) {
        Sprite sprite = null;
        Color color = Color.white;
        switch (c) {
            case ButtonColor.Dissabled:
                sprite = SpriteDissabled;
                color = ColorDissabled;
                break;
            case ButtonColor.Green:
                sprite = SpriteGreen;
                color = ColorGreen;
                break;
            case ButtonColor.Orange:
                sprite = SpriteOrange;
                color = ColorOrange;
                break;
            default:
                break;
        }
        if(sprite != null)
            _image.sprite = sprite;
        _text.color = color;
    }

    private void ChangeStateSize(ButtonSize size) {
        switch (size) {
            case ButtonSize.Small:
                _text.fontSize = 40;
                break;
            case ButtonSize.Medium:
                _text.fontSize = 60;
                break;
            case ButtonSize.Large:
                _text.fontSize = 80;
                break;
            default:
                break;
        }
    }
}