using UnityEngine;

// Any entity that emits light should have this
public class LightEntity : MonoBehaviour {
    public float LightValue;
    private void Start() {
        // So we have a slight problem here, if we want to register this light to the terraforming manager here, we'll
        // Have to know what our persistant ID is, but that would be quite hard to get right? I feel like the IDs should be
        // Handled by the managers and not by the objects themselves, and how would we get the ID here in the first place?
        // 
    }
}
