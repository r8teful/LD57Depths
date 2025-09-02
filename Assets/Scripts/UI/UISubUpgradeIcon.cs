using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Used in the control panel overview, hover over it to see the upgrade, if you click you get sent to the upgrade tab
public class UISubUpgradeIcon : MonoBehaviour, IPopupInfo, IPointerEnterHandler, IPointerExitHandler {
    public event Action PopupDataChanged;
    [SerializeField] private SubRecipeSO _recipeData;
    private UISubPanelOverview _parent;
    private Image _image;
    public enum SubUpgradeState { Available,Unavailable,Upgraded}
    public SubUpgradeState State;
    public SubRecipeSO RecipeData => _recipeData;
    public void Init(UISubPanelOverview parent,SubUpgradeState state) {
        _parent = parent;
        State = state;
        _image = GetComponent<Image>();
        SubmarineManager.Instance.OnUpgradeDataChanged += HandleUpgradeStateChanged;
    }

    private void HandleUpgradeStateChanged(ushort updatedRecipeId) {
        // Check if the event is for US.
        SubUpgradeState state = GetUpgradeStateFromID(SubmarineManager.Instance.CurrentRecipe);
        RefreshVisuals(state);
        
    }

    private SubUpgradeState GetUpgradeStateFromID(ushort currentRecipe) {
        if (currentRecipe == _recipeData.ID) return SubUpgradeState.Available;
        if (currentRecipe > _recipeData.ID) return SubUpgradeState.Upgraded;
        return SubUpgradeState.Unavailable;
    }

    private void RefreshVisuals(SubUpgradeState state) {
        if (_image == null) return;
        ColorUtility.TryParseHtmlString("#EB257B", out var availableColor);

        switch (state) {
            case SubUpgradeState.Available:
            _image.color = availableColor;
            break;
            case SubUpgradeState.Unavailable:
            _image.color = Color.gray; // Gray might be better than white
            break;
            case SubUpgradeState.Upgraded:
            _image.color = Color.green; 
            break;
        }

        // You could also update text, progress bars for contributions, etc. here.
    }
    public PopupData GetPopupData(InventoryManager inv) {
        // We can alternativaly enter the resource amount here if we'd like

        // TODO also we should set the icon to the current "tier" of the upgrade we are in
        
        // Set correct description text
        string description = string.Empty;
        switch (State) {
            case SubUpgradeState.Available:
                description = _recipeData.description;
                break;
            case SubUpgradeState.Unavailable:
                description = "Unlocks after ____ is repaired"; // todo
                break;
            case SubUpgradeState.Upgraded:
                description = "Fully repaired";
                break;
            default:
            break;
        }
        return new PopupData(_recipeData.displayName, description, null, _recipeData.UpgradeIconSteps[0]);
    }

    public void OnPointerEnter(PointerEventData eventData) {
        PopupManager.Instance.ShowPopup(this, true);
    }

    public void OnPointerExit(PointerEventData eventData) {
        PopupManager.Instance.ShowPopup(this, false);
    }
}