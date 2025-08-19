using System;
using UnityEngine;
using UnityEngine.UI;

public class EnvironmentProgressionSlider : MonoBehaviour, IPopupInfo {

    [Header("Slider References")]
    public Slider currentSlider;
    public Slider maxSlider;

    public event Action PopupDataChanged;
    public event Action<IPopupInfo, bool> OnPopupShow;

    void OnEnable() {
        TerraformingManager.Instance.OnTotalChanged += OnTerraformingChange;
        currentSlider.maxValue = 800; // Just arbitary
        maxSlider.maxValue = 800; 

        //delayedSlider.maxValue = maxHealth;

        //mainSlider.value = currentHealth;
        //delayedSlider.value = currentHealth;
        UpdateSlider();
    }

    private void UpdateSlider() {
        var c = TerraformingManager.Instance.GetTotal(TerraformType.Oxygen);
        var max = TerraformingManager.Instance.GetMaxPotential(TerraformType.Oxygen);
        currentSlider.value = c;
        maxSlider.value = max;
    }

    private void OnDisable() {
        TerraformingManager.Instance.OnTotalChanged -= OnTerraformingChange;
        
    }

    private void OnTerraformingChange(TerraformType type, float value) {
        Debug.Log($"TerrfaormingChage! {type} with value {value}");
        currentSlider.value = value;
    }

    private void OnOxygenChange(float prev, float next, bool asServer) {
        throw new System.NotImplementedException();
    }

    public PopupData GetPopupData(InventoryManager inv) {
        throw new NotImplementedException();
    }
}