using UnityEngine;
[RequireComponent(typeof(Renderer))]
public class RandomShaderSeedSetter : MonoBehaviour {
    // Note that this script also creates a new copy of the material
    private Material _material;
    private void Awake() {
        _material =  GetComponent<Renderer>().material;
        GetComponent<Renderer>().material = new Material(_material);
        GetComponent<Renderer>().material.SetFloat("_Seed", Random.Range(0, 9999));
    }
}