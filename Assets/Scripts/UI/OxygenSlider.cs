using UnityEngine;
using UnityEngine.UI;

public class OxygenSlider : MonoBehaviour {
    public Slider oxygenSlider;
    private void Awake() {
        PlayerMovement.OnOxygenChanged += OxygenChanged;
    }

    private void OnDestroy() {    
        PlayerMovement.OnOxygenChanged -= OxygenChanged;
    }

    private void OxygenChanged(float current, float max) {
        oxygenSlider.maxValue = max;
        oxygenSlider.value = current;
    }
}
