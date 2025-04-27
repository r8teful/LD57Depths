using UnityEngine;
using UnityEngine.UI;

public class OxygenSlider : MonoBehaviour {
    public Slider oxygenSlider;
    private void Awake() {
        PlayerController.OnOxygenChanged += OxygenChanged;
    }

    private void OnDestroy() {    
        PlayerController.OnOxygenChanged -= OxygenChanged;
    }

    private void OxygenChanged(float current, float max) {
        oxygenSlider.maxValue = max;
        oxygenSlider.value = current;
    }
}
