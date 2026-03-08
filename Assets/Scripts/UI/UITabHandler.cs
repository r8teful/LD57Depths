using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UITabHandler : MonoBehaviour {
    [System.Serializable]
    public class TabPair {
        public Button tabButton;
        public GameObject tabContent;
    }

    [SerializeField] private RectTransform _activeTabIndicator;
    [SerializeField] private List<TabPair> _tabs = new List<TabPair>();
    [SerializeField] private int _defaultTabIndex = 0;

    private int _currentTabIndex = -1;
    public event Action<int> OnTabSwitched;

    private void Start() {
        InitializeTabs();
    }

   public void InitializeTabs() {
        for (int i = 0; i < _tabs.Count; i++) {
            if (_tabs[i].tabButton == null) continue;
            _tabs[i].tabButton.onClick.RemoveAllListeners();
            int capturedIndex = i;
            _tabs[i].tabButton.onClick.AddListener(() => SwitchTab(capturedIndex));
        }

        if (_tabs.Count > 0 && _defaultTabIndex >= 0 && _defaultTabIndex < _tabs.Count) {
            SwitchTab(_defaultTabIndex);
        }
    }

    public void SwitchTab(int index) {
        if (index == _currentTabIndex) return; 
        _currentTabIndex = index; 

        for (int i = 0; i < _tabs.Count; i++) {
            if (_tabs[i].tabButton == null || _tabs[i].tabContent == null) continue;

            bool isTargetTab = (i == index);
            _tabs[i].tabContent.SetActive(isTargetTab);
            _tabs[i].tabButton.interactable = !isTargetTab;
        }

        OnTabSwitched?.Invoke(index);
        UpdateIndicatorPosition(index);
    }
    public void NextTab() {
        if (_tabs.Count <= 1) return;

        int nextIndex = (_currentTabIndex + 1) % _tabs.Count;
        SwitchTab(nextIndex);
    }

    public void PreviousTab() {
        if (_tabs.Count <= 1) return;

        int prevIndex = _currentTabIndex - 1;
        if (prevIndex < 0) prevIndex = _tabs.Count - 1; // Wrap to the end
        SwitchTab(prevIndex);
    }

    public void SetControllerVisualsActive(bool isActive) {
        //if (leftControllerPrompt != null) leftControllerPrompt.SetActive(isActive);
        //if (rightControllerPrompt != null) rightControllerPrompt.SetActive(isActive);
    }
    private void UpdateIndicatorPosition(int index) {
        if (_activeTabIndicator == null || _tabs[index].tabButton == null) return;

        RectTransform targetButtonRect = _tabs[index].tabButton.GetComponent<RectTransform>();
        _activeTabIndicator.DOKill();
        _activeTabIndicator.transform.DOMoveX(targetButtonRect.position.x, 0.2f).SetEase(Ease.OutBack);
        //_activeTabIndicator.position = targetButtonRect.position;
    }
}
