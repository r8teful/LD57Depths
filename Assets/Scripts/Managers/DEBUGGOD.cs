using UnityEngine;
using UnityEngine.Tilemaps;

public class DEBUGGOD : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Vector3 currentInput;
    private Rigidbody2D rb;
    public WorldManager _worldManager;
    public TileBase _airTile;
    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
    }
    private void Start() {
        // _worldManager = FindFirstObjectByType<WorldManager>();
        //_airTile = _worldManager.GetIDToTile()[0]; // air is id 0
    }

    private void Update() {
        // Get raw input (no smoothing)
        currentInput = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).normalized;
        if (Input.GetMouseButton(0)){
            Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Debug.Log($"Hoovering over: {_worldManager.GetTileAtWorldPos(mouseWorldPosition)} which is tile {_worldManager.WorldToCell(mouseWorldPosition)}");

            _worldManager.SetTileAtWorldPos(mouseWorldPosition, _airTile);
        }
    }

    private void FixedUpdate() {
        // Move the player
        transform.position += currentInput * moveSpeed * Time.fixedDeltaTime;
    }
}
