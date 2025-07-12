using DG.Tweening;
using FishNet.Object;
using System.Collections;
using UnityEngine;

public class CleaningTool : NetworkBehaviour, IToolBehaviour {
    private bool cleaning;
    private InputManager inputManager;
    [SerializeField] private BoxCollider2D _triggerBox;
    public ParticleSystem _particleSystem;
    public SpriteRenderer _visual;
    public LayerMask _wasteMask;
    [Header("Pull Settings")]
    public float range = 5f;
    public float coneAngle = 45f;
    public float pullForce = 10f;

    public GameObject GO => gameObject;

    private void Start() {
        _particleSystem.Stop();
        _visual.enabled = false;
        _triggerBox.enabled = false;
    }
    public void ToolStart(InputManager input, ToolController controller) {
        if (base.IsOwner) {
            inputManager = input; // Store input manager localy
            CmdStartCleaning();
            _particleSystem.Play();
            _visual.enabled = true;
            _triggerBox.enabled = true;
            if (!base.IsServerInitialized) {
                // local visuals on client
                cleaning = true; 

            } 
        }
    }
    [ServerRpc(RequireOwnership = true)]
    private void CmdStartCleaning() {
        // Server side stuff
        cleaning = true;
    }
    [ServerRpc(RequireOwnership = true)]
    private void CmdStopCleaning() {
        // Server side stuff
        cleaning = false;
    }
    private IEnumerator CleaningRoutine(InputManager input, ToolController controller) {
        while (true) {
            //laser.volume = 0.2f;
            var pos = input.GetAimInput();
            //Debug.Log(pos);
            CastRays(pos); // Todo determine freq here
            CleaningVisual(pos);
            yield return new WaitForFixedUpdate();
            //laser.volume = 0f;
        }
    }

    public void ToolStop() {
        CmdStopCleaning();
        _particleSystem.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        //laser.volume = 0.0f;
        _visual.enabled = false;    
    }

    private void CastRays(Vector2 pos) {
        Vector2 objectPos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 directionToMouse = (pos - objectPos2D).normalized;
        Collider2D[] hits = Physics2D.OverlapCircleAll(objectPos2D, range, _wasteMask);

        foreach (Collider2D hit in hits) {
            Vector2 toTarget = ((Vector2)hit.transform.position - objectPos2D).normalized;
            float angleToTarget = Vector2.Angle(directionToMouse, toTarget);
            if (angleToTarget <= coneAngle / 2f) {
                Rigidbody2D rb = hit.attachedRigidbody;
                if (rb != null) {
                    Vector2 forceDir = (objectPos2D - rb.position).normalized;
                    rb.AddForce(forceDir * pullForce, ForceMode2D.Force);
                }
            }
        }
    }
    private void CleaningVisual(Vector2 pos) {
        //Debug.Log("Trying to look at:" + pos);
        Vector2 objectPos2D = new Vector2(transform.position.x, transform.position.y);
        Vector2 directionToMouse = (pos - objectPos2D).normalized;
        //Vector3 direction = mouseWorldPos - transform.position;
        float angle = Mathf.Atan2(directionToMouse.y, directionToMouse.x) * Mathf.Rad2Deg;
        transform.GetChild(0).rotation = Quaternion.Euler(0, 0, angle+90);
    }

    private void FixedUpdate() {
        if (base.IsServerInitialized && cleaning) {
            CastRays(inputManager.GetAimInput());
        }
    }
    private void Update() {
        if (cleaning) {
            // Visuals!
            CleaningVisual(inputManager.GetAimInput());
        }
    }

    internal void OnTriggerWasteEnter(GameObject gameObject) {
        // Visually shrink and pickup (should be networked later)
        gameObject.transform.DOScale(0, 0.5f).OnComplete(() => Destroy(gameObject)) ;
    }

    // Clean tool always hidden for now
    public void ToolHide() {
    }

    public void ToolShow() {
    }
}