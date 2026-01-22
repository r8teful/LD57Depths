using System;
using UnityEngine;
using UnityEngine.UI;

public abstract class UIHudIconBase : MonoBehaviour, IPopupInfo {
    [SerializeField] protected Image _iconImage;
    private string _descText;
    private string _headerText;

    public Vector2 LookaheadPos => Vector2.zero;

    public event Action PopupDataChanged;
    public PopupData GetPopupData(InventoryManager inv) {
        return new PopupData(_headerText, _descText, null);
    }

    public void InitBase(Sprite icon, string headerText, string description) {
        _iconImage.sprite = icon;
        _headerText = headerText;
        _descText = description;
    }
}