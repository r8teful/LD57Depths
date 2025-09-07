using System;
using UnityEngine;
using UnityEngine.UI;

// Right now we have one of these scripts attached to each of the buttons in the control panel UI, which sounds like a stupid idea but maybe not?
public class UITrenchZone : MonoBehaviour {
    public ZoneSO ZoneData;
    [SerializeField] private Image _trenchBoxImage;
    private Button _button;
    private UISubMap _parent;
    internal void Init(UISubMap uISubMap) {
        _parent = uISubMap;
    }

    internal void SetColor(Color color) {
        _trenchBoxImage.color = color;
    }

    internal void SetInteractable(bool b) {
        _button.interactable = b;
    }

    private void Awake() {
        _button = GetComponent<Button>();
        if (_button != null) {
            _button.onClick.AddListener(OnMapButtonClicked);
        }
    }
    private void OnDestroy() {
        if (_button != null) {
            _button.onClick.RemoveListener(OnMapButtonClicked);
        }
    }
    private void OnMapButtonClicked() {
        _parent.OnMapButtonClicked(ZoneData);
    }
}