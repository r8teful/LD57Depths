using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DEBUGGOD : MonoBehaviour, IToolBehaviour {
    public float moveSpeed = 5f;

    private Vector3 currentInput;
    private Rigidbody2D rb;
    public WorldManager _worldManager;
    public ChunkManager _chunkManager;
    public TileBase _airTile;
    private bool _isDamaging = false;
    public GameObject GO => null;

    public IToolVisual ToolVisual => throw new System.NotImplementedException();

    public ToolType ToolType => ToolType.GOD;

    public ushort ToolID => (ushort)ToolType;

    public MiningToolData GetToolData() {
        throw new System.NotImplementedException();
    }

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
    }
 
    private IEnumerator DamageTileRoutine(InputManager input) {
        _isDamaging = true;
        while (_isDamaging) {
            Vector3 mouseWorldPosition = input.GetAimWorldInput();
            Vector3Int targetCell = _worldManager.WorldToCell(mouseWorldPosition);

            short damageAmount = 5; // Replace with player tool value
            CmdRequestDamageTile(targetCell, damageAmount);

            yield return null; 
        }
    }
    private void CmdRequestDamageTile(Vector3Int cellPosition, short damageAmount) {
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

    public void ToolStart(InputManager manager, ToolController controller) {
        StartCoroutine(DamageTileRoutine(manager));
    }

    public void ToolStop(ToolController controller) {
        _isDamaging = false;
    }

    public void ToolAbilityStart(ToolController toolController) {
        throw new System.NotImplementedException();
    }

    public void ToolAbilityStop(ToolController toolController) {
        throw new System.NotImplementedException();
    }

    public void InitVisualTool(IToolBehaviour toolBehaviourParent, NetworkedPlayer owningPlayer) {
        throw new System.NotImplementedException();
    }
}
