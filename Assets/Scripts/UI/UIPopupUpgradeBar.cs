using System;
using UnityEngine;
using UnityEngine.UI;

public class UIPopupUpgradeBar : MonoBehaviour {
    [SerializeField] private Image _barProgressImage; // In the button 
    
    public void UpdateVisuals(int maxLevel, int currLevel) {
        if (maxLevel > 0) {
            float raw = (float)currLevel / maxLevel;
            _barProgressImage.fillAmount = Mathf.Floor(raw * 10f) / 10f;
        } else {
            _barProgressImage.fillAmount = 0;
        }
    }

    public void UpdateVisuals(NodeProgressionStatus progressionInfo) {
        UpdateVisuals(progressionInfo.LevelMax, progressionInfo.LevelCurr);
    }
}
