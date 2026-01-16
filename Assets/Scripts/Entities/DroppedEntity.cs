using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class DroppedEntity : MonoBehaviour {
    private ItemData _cachedItemData;
    private ushort _itemID;
    private float timeSinceSpawned;

    public ushort ItemID => _itemID;

    public bool IsPicked { get; internal set; }

    [SerializeField] private SpriteRenderer spriteRenderer;
    private Rigidbody2D _rb;
    private float _gravityScale;

    private void Awake() {
        _rb = GetComponent<Rigidbody2D>();
        _gravityScale = _rb.gravityScale;
    }
    public void Init(ushort id, int quantity) {
        _cachedItemData = App.ResourceSystem.GetItemByID(id);
        _itemID= id;
        timeSinceSpawned = 0f;
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
        _rb.gravityScale = _gravityScale;
    }
}