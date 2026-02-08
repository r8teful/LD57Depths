using UnityEngine;
using UnityEngine.UI;

public class UIPopupUpgradeBar : MonoBehaviour {
    [SerializeField] private Image _barProgressImage; // In the button 
    [SerializeField] private Image _upgradeBarDivider; // Instantiates these
    [SerializeField] private Transform _dividerLayout; 
    public void UpdateVisuals(int maxLevel, int currLevel) {
        if (maxLevel > 0) {
            float raw = (float)currLevel / maxLevel;
            _barProgressImage.fillAmount = raw;
        } else {
            _barProgressImage.fillAmount = 0;
        }
        CreateDividers(maxLevel);
    }

    private void CreateDividers(int maxLevel) {
        foreach (Transform child in _dividerLayout) {
            // we need one gameobject in the layout group for it to work properly
            if (child.gameObject.name == "NOTOUCH") continue; 
            Destroy(child.gameObject);
        }
        for (int i = 0; i < maxLevel-1; i++) {
            Instantiate(_upgradeBarDivider, _dividerLayout);
        }

    }

    public void UpdateVisuals(NodeProgressionStatus progressionInfo) {
        UpdateVisuals(progressionInfo.LevelMax, progressionInfo.LevelCurr);
    }
}
