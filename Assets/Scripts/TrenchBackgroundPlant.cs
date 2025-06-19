using UnityEngine;

public class TrenchBackgroundPlant : MonoBehaviour {
    [SerializeField] private SpriteRenderer _spriteRenderer;
    void Start() {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update() {
        _spriteRenderer.material.SetVector("_CamPos", Camera.main.transform.position);
    }
}