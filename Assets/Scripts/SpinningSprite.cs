using UnityEngine;

public class SpinningSprite : MonoBehaviour
{
    [SerializeField] float degreesPerSecond = 180f;

    void Update() {
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
    }
}
