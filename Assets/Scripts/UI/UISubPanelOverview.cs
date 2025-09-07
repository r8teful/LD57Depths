using UnityEngine;
using UnityEngine.UI;

public class UISubPanelOverview : MonoBehaviour {
    [SerializeField] private UISubUpgradeIcon[] upgradeIcons; // IMPORTANT!! Order matters
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
        // Init the upgradeIcons with their state Available/Unavailable/Upgraded
        for (int i = 0; i < upgradeIcons.Length; i++) {
            // Do this if no save data exists, otherwise, load the save data and set it like that
            upgradeIcons[i].Init(this, i == 0 ? UISubUpgradeIcon.SubUpgradeState.Available : UISubUpgradeIcon.SubUpgradeState.Unavailable);
            
        }
    }
    public void OnEnabledUpgradeIconClicked() {
        _parent.OnTabButtonClicked(1); // uggly but tab 1 is upgrades 
    }
    public SubRecipeSO GetSubRecipeData(int upgradeIndex) {
        return upgradeIcons[upgradeIndex].RecipeData;
    }

}