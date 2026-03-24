using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class UIWorldCanvasCameraAspect : MonoBehaviour {
    [SerializeField] private float referenceHeight = 1080f;
    [SerializeField] private float maxWidth = 1920f;

    private RectTransform _rectTransform;

    private void Awake() {
        _rectTransform = GetComponent<RectTransform>();
        ApplyAspect();
    }

    private void OnEnable() {
        _rectTransform = GetComponent<RectTransform>();
        ApplyAspect();
    }

    private void Update() {
        ApplyAspect();
    }

    private void ApplyAspect() {
        if (_rectTransform == null) return;

        float aspect = (float)Screen.width / Screen.height;
        float width = referenceHeight * aspect;
        _rectTransform.sizeDelta = new Vector2(Mathf.Min(width, maxWidth), referenceHeight);
    }
}
