using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

// This is on the interior instance
public class SubmarineManager : StaticInstance<SubmarineManager> {
     private int _currentZoneIndex;
    public int CurrentZoneIndex => _currentZoneIndex;

    // A client-side event that the UI can subscribe to.
    public event Action<ushort> OnUpgradeDataChanged; // Passes the RecipeID that changed
    public event Action<ushort> OnCurRecipeChanged; // Passes the new RecipeID 
    public event Action OnSubMoved; // Used by map 
    public GameObject submarineExterior;
    public Transform InteriorSpawnPoint;
    [ShowInInspector]
    private InventoryManager subInventory;
    public InventoryManager SubInventory => subInventory;
    [SerializeField] private SubItemGainVisualSpawner itemGainSpawner;

    private HashSet<ushort> _subUpgrades = new HashSet<ushort>(); // UPGRADE NODE ID 
    [SerializeField] private List<UpgradeNodeSO> _majorUpgrades; // When the player gets this recipe, we increase the "stage"
    
    [SerializeField] private Transform _cutsceneCameraPosUpgradeMachine;
    [SerializeField] private Transform _cutsceneCameraPosControlPanel; 
    [SerializeField] private SpriteRenderer _upgradeMachine; 
    
    protected override void Awake() {
        base.Awake();
        subInventory = new InventoryManager();
        itemGainSpawner.Init(subInventory);
    }
    public void MoveSub(int index) {
        submarineExterior.transform.position = new(0, GameSetupManager.Instance.WorldGenSettings.GetWorldLayerYPos(index));
        SetSubPosIndex(index);
    }
    internal void SetSubPosIndex(int index) {
        _currentZoneIndex = index;
        OnSubMoved?.Invoke();
    }

    public void Start() {
        var y = GameSetupManager.Instance.WorldGenSettings.MaxDepth;
        submarineExterior.transform.position = new Vector3(0, y);
    }

    internal void NewSubUpgrade(SubUpgradeEffect effect) {
        if (effect.isMajor) {
            bool success = _subUpgrades.Add(effect.upgrade.ID);
            if (!success) {
                Debug.LogError("Sub recipe already purchased! Did you assigned a unique ID?");
                return;
            }
            HandleCutscene(effect.upgrade.ID, effect,ResourceSystem.SubUpgradePanel, _cutsceneCameraPosUpgradeMachine);
        }
        // TODO update visual of the sub based on effect sprites

    }

    private void HandleCutscene(ushort newUpgrade, SubUpgradeEffect effect, ushort ID, Transform cameraPos) {
        if (newUpgrade == ID) {
            GameSequenceManager.Instance.AddEvent(
                onStart: () => {
                    GameCutsceneManager.Instance.StartSubUpgradeCutscene(
                        cameraPos,
                        () => _upgradeMachine.sprite = effect.SpriteInterior
                        );
                },
            onFinish: () => {

            }
           );
        }
    }
    private void ChangeUpgradeSprite() {

    }

    internal void MoveInterior(VisibilityLayerType currentLayer) {
        if (currentLayer == VisibilityLayerType.Interior) {
            // Move in
            transform.position = submarineExterior.transform.position;
        } else {
            // Move out lol
            Vector3 YEET = new(6000, 0);
            transform.position = submarineExterior.transform.position + YEET;
        }
    }

    public int GetUpgradeStage() => _subUpgrades.Count; // lol?
    
    public bool CanMoveTo(int index) {
        return index <= GetUpgradeStage();
    }

    // FOR DEBUGING PUPROSES!!
    internal void RemoveAllUpgrades() {
        _subUpgrades.Clear();
    }
}