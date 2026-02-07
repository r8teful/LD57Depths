using UnityEngine;

public class TilePlant : MonoBehaviour, ITileChangeReactor {

    [SerializeField] private float groundCheckDistance = 0.6f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform offset;
    private bool isGrounded = true; // Server state
    private Rigidbody2D rb;
    private Vector3Int cellPos;
    private GameObject spriteGameobject;

    
    public void OnTileChangedNearby(Vector3Int cellPosition, int newTileID) {
        if (newTileID == 0) {
            if(cellPosition == cellPos) {
                Debug.Log("on tile!!");
                Destroy(gameObject); // This might need to be a pool aswell later
                var item = App.ResourceSystem.GetItemByID(ResourceSystem.BiomeEssenceID);
                WorldDropManager.Instance.SpawnDropOne(spriteGameobject.transform.position, 1, item);
            }
        }
    }
    private void Awake() {
        rb = GetComponent<Rigidbody2D>();
        if (rb) rb.bodyType = RigidbodyType2D.Kinematic; // Start kinematic
        MovePos();
        cellPos = new Vector3Int(Mathf.RoundToInt(transform.position.x - 0.5f), Mathf.RoundToInt(transform.position.y - 0.5f));
        spriteGameobject = GetComponentInChildren<SpriteRenderer>().gameObject;
    }
    public void Start() {
       // CheckGroundedState(); // Initial check
    }
    /*
        // --- Called by EntityManager via Interface ---
        [Server] // Ensure server execution
        public void OnTileChangedNearby(Vector3Int cellPosition, int newTileID) {
            // Check if the change happened directly below us
            if (newTileID == 0) {
                if (cellPosition == transform.position) {
                    // The tile directly below us changed! Re-check if we are grounded.
                    CheckGroundedState();
                }
            }
        }*/

    // Move depending of the orientation of the root
    private void MovePos() {
        // BRUH, it's all bloddy the same because when we rotate it the local up will be the right way up 
        var angles = transform.localEulerAngles;
        offset.localPosition = new Vector3(0f, 0.5f, 0f);
    }
    private void CheckGroundedState() {
        // Raycast down slightly further than foot position
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, groundCheckDistance, groundLayer);
        bool nowGrounded = hit.collider != null;

        if (isGrounded && !nowGrounded) {
            // Was grounded, now isn't -> Start falling!
            isGrounded = false;
            Debug.Log($"{gameObject.name} lost ground, starting to fall!");
            if (rb) rb.bodyType = RigidbodyType2D.Dynamic; // Enable physics
                                                           // Client visual update handled by NetworkTransform syncing Rigidbody
        } else if (!isGrounded && nowGrounded) {
            // Was falling, now grounded -> Stop falling
            isGrounded = true;
            Debug.Log($"{gameObject.name} landed!");
            if (rb) rb.bodyType = RigidbodyType2D.Kinematic; // Disable physics, maybe reset velocity
                                                             // Potentially snap to ground slightly? rb.position = ... hit.point ... ?
        }
        // Else no change in grounded state
    }
   
}