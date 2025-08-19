using System;
using UnityEngine;
using UnityEngine.UI;

public class EnvironmentProgressionSlider : MonoBehaviour {

    [Header("Slider References")]
    public Slider mainSlider;
    public Slider delayedSlider;

    void OnEnable() {
        TerraformingManager.Instance.OnTotalChanged += OnTerraformingChange;
        //mainSlider.maxValue = maxHealth;
        //delayedSlider.maxValue = maxHealth;

        //mainSlider.value = currentHealth;
        //delayedSlider.value = currentHealth;
        UpdateSlider();
    }

    private void UpdateSlider() {
        var f = TerraformingManager.Instance.GetTotal(TerraformType.Oxygen);
        mainSlider.value = f;
    }

    private void OnDisable() {
        TerraformingManager.Instance.OnTotalChanged -= OnTerraformingChange;
        
    }

    private void OnTerraformingChange(TerraformType type, float value) {
        Debug.Log($"TerrfaormingChage! {type} with value {value}");
        mainSlider.value = value;
    }

    private void OnOxygenChange(float prev, float next, bool asServer) {
        throw new System.NotImplementedException();
    }
}