using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DEBUGGOD : MonoBehaviour
{
    public float moveSpeed = 5f;

    private Vector3 currentInput;
    private Rigidbody2D rb;
    public WorldManager _worldManager;
    public ChunkManager _chunkManager;
    public TileBase _airTile;
    private bool _toggle;
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
        if (Input.GetMouseButton(0) && !_isDamaging) {
            StartCoroutine(DamageTileRoutine());
            //Debug.Log($"Hoovering over: {_worldManager.GetTileAtWorldPos(mouseWorldPosition)} which is tile {_worldManager.WorldToCell(mouseWorldPosition)}");
            //_worldManager.SetTileAtWorldPos(mouseWorldPosition, _airTile);
            int damageAmount = 1; // Get from player's tool later
        }
    
        if (Input.GetMouseButtonUp(0)) {
            _isDamaging = false;
        }
        if (Input.GetKeyDown(KeyCode.L)) {
            ToggleArea(_toggle);
            _toggle = !_toggle;
        }
    }
    private bool _isDamaging = false;
    public void ToggleArea(bool isWorld) {
        _worldManager.ToggleWorldTilemap(isWorld);
    }
    private IEnumerator DamageTileRoutine() {
        _isDamaging = true;

        while (_isDamaging && Input.GetMouseButton(0)) {
            Vector3 mouseWorldPosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int targetCell = _worldManager.WorldToCell(mouseWorldPosition);

            int damageAmount = 1; // Replace with player tool value
            CmdRequestDamageTile(targetCell, damageAmount);

            yield return new WaitForSeconds(0.05f); 
        }
    }
    //  [ServerRpc]
    private void CmdRequestDamageTile(Vector3Int cellPosition, int damageAmount) {
        if (_worldManager != null) {
            // TODO: Server-side validation (range, tool, cooldowns, etc.)

            // Pass request to WorldGenerator for processing
            _chunkManager.ServerProcessDamageTile(cellPosition, damageAmount); // Pass Owner for potential targeted feedback
        }
    }

    private void FixedUpdate() {
        // Move the player
        transform.position += currentInput * moveSpeed * Time.fixedDeltaTime;
    }
}
