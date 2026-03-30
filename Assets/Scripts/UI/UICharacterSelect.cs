using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UICharacterSelect : MonoBehaviour {

    [SerializeField] private List<CharacterTabPair> _characterTabs = new List<CharacterTabPair>();
    private int _currentTabIndex;
    [SerializeField] private TextMeshProUGUI _textCharacter;
    [SerializeField] private TextMeshProUGUI _textDescription;
    [SerializeField] private TextMeshProUGUI _textTool;
    [SerializeField] private TextMeshProUGUI _textWins;
    //[SerializeField] private SpriteRenderer _characterSprite;
    [SerializeField] private Image _characterSprite;
    [SerializeField] private Sprite _characterUnknownSprite;


  [System.Serializable]
    public class CharacterTabPair {
        public Button characterButton;
        public CharacterDataSO characterData;
    }
    private void Start() {
        InitializeTabs();
    }
    public void InitializeTabs() {
        for (int i = 0; i < _characterTabs.Count; i++) {
            if (_characterTabs[i].characterButton == null) continue;
            _characterTabs[i].characterButton.onClick.RemoveAllListeners();
            int capturedIndex = i;
            _characterTabs[i].characterButton.onClick.AddListener(() => SwitchTab(capturedIndex));
        }
        SwitchTab(0);
    }
    public void SwitchTab(int index) {
        if (index == _currentTabIndex) return;
        _currentTabIndex = index;

        for (int i = 0; i < _characterTabs.Count; i++) {
            if (_characterTabs[i].characterButton == null) continue;

            bool isTargetTab = (i == index);
            if(isTargetTab) SetCharacterData(_characterTabs[i].characterData);
            _characterTabs[i].characterButton.interactable = !isTargetTab;
        }

        UpdateIndicatorPosition(index);
    }

    private void SetCharacterData(CharacterDataSO d) {
        // You'd localise here
        if(d == null) {
            _textCharacter.text = "???";
            _textDescription.text = "???";
            _textTool.text = "???";
            _textWins.text = "";
            _characterSprite.sprite = _characterUnknownSprite;
            return;
        }
        _textCharacter.text = d.characterNameKey;
        _textDescription.text = d.characterDescriptionKey;
        _textTool.text = d.characterToolKey;
        _textWins.text = "0"; // TODO fetch from stats manager
        
        _characterSprite.sprite = d.characterSprite;
    }

    private void UpdateIndicatorPosition(int index) {
        // todo
    }
}