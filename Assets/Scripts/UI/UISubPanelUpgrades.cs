using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISubPanelUpgrades : MonoBehaviour {
    [SerializeField] private Image _upgradeStatusImage;
    [SerializeField] private Image _upgradeStatusImageCompletionWhite;
    [SerializeField] private TextMeshProUGUI _upgradeName;
    [SerializeField] private Transform _upgradeBarContainer;
    private Sprite _currentUpgradeStageSprite;
    private Dictionary<ushort,UISubUpgradeBar> _upgradeBars = new Dictionary<ushort, UISubUpgradeBar>(); // Runtime instantiated bars
    private SubRecipeSO _curRecipeData;
    private void Awake() {
        SubmarineManager.Instance.OnUpgradeDataChanged += UpgradeDataChanged;
        SubmarineManager.Instance.OnCurRecipeChanged += CurRecipeChanged;
        // Fetch the current upgrade & its state
        ushort curRecipe = SubmarineManager.Instance.CurrentRecipe;
        var recipeData = App.ResourceSystem.GetRecipeByID(curRecipe);
        _curRecipeData = recipeData as SubRecipeSO;
        InitializeUpgradeBars();
        UpdatePanelVisuals();
    }

 

    // Initializes the empty state of a recipeBar.
    private void InitializeUpgradeBars() {
        // Delete existing 
        for (int i = 0; i < _upgradeBarContainer.childCount; i++) {
            Destroy(_upgradeBarContainer.GetChild(i).gameObject);
        }
        _upgradeBars.Clear();

        // Now here we want to instantiate the progress bars, and initialize them with how many resources we have
        foreach (var reqItem in _curRecipeData.requiredItems) {
            // RequiredAmount is how much is contributed when pressing the button
            int requiredAmount = Mathf.CeilToInt((float)reqItem.quantity / 10); // Maybe later this wont be 8, also idealy this would need to be divisable by 8 
            var ingredientStatus = new IngredientStatus(reqItem.item, requiredAmount,
                NetworkedPlayer.LocalInstance.GetInventory().GetItemCount(reqItem.item.ID));
            var contributed = SubmarineManager.Instance.GetContributedAmount(_curRecipeData.ID, reqItem.item.ID);
            var bar = Instantiate(App.ResourceSystem.GetPrefab<UISubUpgradeBar>("UISubUpgradeBar"), _upgradeBarContainer);
            bar.Init(this,_curRecipeData, ingredientStatus, contributed, reqItem.quantity);
            _upgradeBars.Add(reqItem.item.ID, bar);
        }
    }

    private void UpgradeDataChanged(ushort recipeChangedID) {
        UpdateBarVisuals();
        UpdatePanelVisuals();
    }
    private void CurRecipeChanged(ushort id) {
        SubRecipeSO changedRecipe = App.ResourceSystem.GetRecipeByID(id) as SubRecipeSO;
        if (_curRecipeData != changedRecipe) {
            // Play animation
            StartCoroutine(NewRecipeRoutine(changedRecipe));
        }
    }
    private IEnumerator NewRecipeRoutine(SubRecipeSO recipe) {
        UpdatePanelVisuals();
        UpdateBarVisuals();
        // start shaking and transition too white
        var duration = 2f;
        _upgradeStatusImageCompletionWhite.DOFade(1, duration);
        _upgradeStatusImage.rectTransform.DOShakePosition(duration,0.4f);
        Vector3 strenght = new(5, 0, 5);
        _upgradeStatusImage.rectTransform.DOShakeRotation(duration,strenght,randomnessMode: ShakeRandomnessMode.Harmonic);
        yield return new WaitForSeconds(duration); // Wait for anim to finish
        UpdatePanelVisuals(); // Now actually show the new sprite
        _upgradeStatusImage.sprite = _currentUpgradeStageSprite;
        _upgradeStatusImage.rectTransform.DORotate(new(0, 0, 0), duration * 0.4f); // rotate back because shake doesn't do that 
        yield return new WaitForSeconds(duration); // wait after the animation to show the pretty new image

        _curRecipeData = recipe;
        // Now actually delete old subupgradeBars, and instatiate new ones
        InitializeUpgradeBars();
        UpdatePanelVisuals(); // This will now uppdate  _currentUpgradeStageSprite
        // Here we actually set the sprite because we don't want to do it everytime in UpdatePanelVisual,  
        _upgradeStatusImage.sprite = _currentUpgradeStageSprite;
    }

    private void UpdatePanelVisuals() {
        int upgradeIndex = SubmarineManager.Instance.GetUpgradeIndex(_curRecipeData.ID);
        _currentUpgradeStageSprite = _curRecipeData.UpgradeIconSteps[upgradeIndex];
        _upgradeStatusImageCompletionWhite.color = new(1,1,1,0);
        _upgradeName.text = _curRecipeData.displayName;
    }

    private void UpdateBarVisuals() {
        // send the new IngredientStatus, and item totals to all upgrade bars... 
        foreach (var reqItem in _curRecipeData.requiredItems) {
            int requiredAmount = Mathf.CeilToInt((float)reqItem.quantity / 10); // Have to find a better way to get this
            var ingredientStatus = new IngredientStatus(reqItem.item, requiredAmount,
            NetworkedPlayer.LocalInstance.GetInventory().GetItemCount(reqItem.item.ID));
            var contributed = SubmarineManager.Instance.GetContributedAmount(_curRecipeData.ID, reqItem.item.ID);
            _upgradeBars[reqItem.item.ID].SetNewData(ingredientStatus, contributed); // Easy lookup now that we've mapped the item ID to each bar
        }
    }

    // omg this is so messy now we set the image only after this lil animation, but the rest of the info is updated
    // in UpdatePanelVisuals through the event. EHHH Idk about that but ehhhh
    internal void BarCompleteAnimation() {
        StartCoroutine(BarCompleteAnim());
    }
    private IEnumerator BarCompleteAnim() {
        var dur = 0.2f;
        _upgradeStatusImageCompletionWhite.DOFade(1, dur).SetEase(Ease.InQuart);
        yield return new WaitForSeconds(dur);
        _upgradeStatusImage.sprite = _currentUpgradeStageSprite;
        _upgradeStatusImageCompletionWhite.DOFade(0, dur).SetEase(Ease.OutQuart);
    
    }
}