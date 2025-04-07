using UnityEngine;

public class Parallax : MonoBehaviour {
    private float length, startpos;
    public GameObject Camera;
    public float parallaxEffect;
    private void Start() {
        startpos = transform.position.y;
        length = GetComponent<SpriteRenderer>().bounds.size.y;
    }
    private void FixedUpdate() {
        float dist = Camera.transform.position.y * parallaxEffect;
        transform.position = new Vector3(transform.position.x, startpos + dist, transform.position.z);
    }
}
