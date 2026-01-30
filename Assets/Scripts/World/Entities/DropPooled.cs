using UnityEngine;

public class DropPooled : MonoBehaviour {
    private ushort _itemID;
    private int _amount;
    public ushort ItemID => _itemID;
    public int Amount => _amount;
    public bool IsPicked { get; internal set; }

    [SerializeField] private SpriteRenderer spriteRenderer;
    private Rigidbody2D _rb;
    private float _gravityScaleCached;

    private void Awake() {
        _rb = GetComponent<Rigidbody2D>();
    }
    public void Init(ItemData data, int amount) {
        _itemID= data.ID; // Inventory manager needs this 
        _amount = amount; // One drop can represent several quantities 
        spriteRenderer.sprite = data.droppSprite;
        _rb.linearDamping = data.linearDamping;
        _rb.gravityScale = data.droppGravityScale;
        _gravityScaleCached = data.droppGravityScale;

    }
    private void OnDisable() {
        // So momentum doesn't carry over to the next spawn
        _rb.linearVelocity = Vector2.zero;
        _rb.angularVelocity = 0f;
        transform.rotation = Quaternion.identity;
        IsPicked = false;
    }

    public void OnStartMagnetizing(Vector2 toCenter,float strength) {
        //_rb.angularVelocity = 0f;
        _rb.freezeRotation = true;
        _rb.gravityScale = 0;
        // finally apply force to change velocity towards desired velocity
        _rb.AddForce(_rb.mass * strength * toCenter, ForceMode2D.Force);
    }

    public void OnStopMagnetizing() {
        _rb.freezeRotation = false;
        _rb.gravityScale = _gravityScaleCached;
    }
}