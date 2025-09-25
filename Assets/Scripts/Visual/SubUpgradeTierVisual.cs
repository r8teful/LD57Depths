using System;
using UnityEngine;

public class SubUpgradeTierVisual : MonoBehaviour {
    [SerializeField] private SpriteRenderer _spriteRenderer;
    [SerializeField] private SubRecipeSO.SubUpgradeType UpgradeType;
    [SerializeField] private bool isInterior;

    private void Start() {
        SubmarineManager.Instance.OnUpgradeDataChanged += HandleUpgradeChange;
        // Fetch the current upgrade & its state
        ushort curRecipe = SubmarineManager.Instance.CurrentRecipe;
        HandleUpgradeChange(curRecipe); // set initial sprite
    }

    private void HandleUpgradeChange(ushort recipe) {
        var r = App.ResourceSystem.GetRecipeByID(recipe);
        if (r is SubRecipeSO subrecipe) {
            if (subrecipe.UpgradeType != UpgradeType) return;
            if (!HasValidVisuals(subrecipe)) return;
            int stage = SubmarineManager.Instance.GetUpgradeIndex(recipe);
            SetSprite(subrecipe, stage);
        }
    }

    private void SetSprite(SubRecipeSO subrecipe, int stage) {
        Sprite sprite;
        if (isInterior) {
            sprite = subrecipe.UpgradeInteriorSteps[stage];
        } else {
            sprite = subrecipe.UpgradeExteriorSteps[stage];
        }
        _spriteRenderer.sprite = sprite;
    }

    private bool HasValidVisuals(SubRecipeSO subrecipe) {
        if (isInterior) { // If we are interior and there are interior visuals
            if (subrecipe.UpgradeInteriorSteps.Length > 0) {
                return true;
            }
        } else {
            // Exterior
            if (subrecipe.UpgradeExteriorSteps.Length > 0) {
                return true;
            }
        }
        return false;
    }
}