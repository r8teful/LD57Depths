using UnityEngine;
using UnityEngine.UI;

public class UISubPanelOverview : MonoBehaviour {
    [SerializeField] private Button mapButton; 
    private UISubControlPanel _parent;
    internal void InitParent(UISubControlPanel parent) {
        _parent = parent;
    }
    private void Awake() {
        mapButton.onClick.AddListener(OnMapButtonClicked);
    }
    private void OnDestroy() {
        mapButton.onClick.RemoveListener(OnMapButtonClicked);
    }
    private void OnMapButtonClicked() {
        _parent.OnTabButtonClicked(2); // uggly but tab 2 is movement
    }

    private void Start() {
     }
    public void OnEnabledUpgradeIconClicked() {
        _parent.OnTabButtonClicked(1); // uggly but tab 1 is upgrades 
    }

}