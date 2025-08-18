using UnityEngine;

public class MiningDrill : MiningBase, IToolBehaviour {
    
    public bool CanMine { get; private set; }
    public override GameObject GO => gameObject;
    public override float Range { get; set; } = 2f;
    public override float DamagePerHit { get; set; } = 5f;

    private IToolVisual _toolVisual;
    public override IToolVisual toolVisual => _toolVisual;

    public override ToolType toolType => ToolType.Drill;

    [SerializeField] private GameObject handVisual;
    private void Awake() {
        Debug.Log("Awake called on miningDrill");
        if (gameObject.TryGetComponent<IToolVisual>(out var c)) {
            _toolVisual = c;
        } else {
            Debug.LogError("Could not find minglazerVisual on gameobject!");
        }
    }
    //public override void ToolStart(InputManager input, ToolController controller) {
    //    base.ToolStart(input, controller);
    //    handVisual.SetActive(true);
    //    NetworkedPlayer.LocalInstance.PlayerVisuals.SetBobHand(false);
    //}
    //public override void ToolStop() {
    //    base.ToolStop();
    //    handVisual.SetActive(false);
    //    // PlayerVisual set sprite to hand
    //    NetworkedPlayer.LocalInstance.PlayerVisuals.SetBobHand(true);
    //}
}