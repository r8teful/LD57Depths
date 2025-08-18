using UnityEngine;
using UnityEngine.UI;

public class EnvironmentProgressionSlider : MonoBehaviour {

    [Header("Slider References")]
    public Slider mainSlider;
    public Slider delayedSlider;

    void Start() {
        TerraformingManager.Instance.CurrentOxygen.OnChange += OnOxygenChange;
        //mainSlider.maxValue = maxHealth;
        //delayedSlider.maxValue = maxHealth;

        //mainSlider.value = currentHealth;
        //delayedSlider.value = currentHealth;
    }

    private void OnOxygenChange(float prev, float next, bool asServer) {
        throw new System.NotImplementedException();
    }
}