using UnityEngine;
using UnityEngine.UI;

public class OxygenSlider : MonoBehaviour {
    public Slider oxygenSlider;
    private void Awake() {
        OxygenManager.OnOxygenChanged += OxygenChanged;
    }

    private void OnDestroy() {
        OxygenManager.OnOxygenChanged -= OxygenChanged;
    }

    private void OxygenChanged(float current, float max) {
        oxygenSlider.maxValue = max;
        oxygenSlider.value = current;
    }
}
