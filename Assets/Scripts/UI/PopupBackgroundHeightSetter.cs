using UnityEngine;

public class PopupBackgroundHeightSetter : MonoBehaviour {
    private RectTransform rect;
    private void Start() {
        rect = GetComponent<RectTransform>();
    }
   // void FixedUpdate() {
   //     //var rect = rect.
   //     var newHeight = transform.parent.GetComponent<RectTransform>().sizeDelta.y;
   //     // Anchored at the top, so offsetMax.y stays the same (usually 0)
   //     // We set offsetMin.y to -height to make it the correct height
   //     Vector2 offsetMin = rect.offsetMin;
   //     offsetMin.y = -newHeight;
   //     rect.offsetMin = offsetMin;
   //
   //     Vector2 offsetMax = rect.offsetMax;
   //     offsetMax.y = 0;
   //     rect.offsetMax = offsetMax;
   // }

}
