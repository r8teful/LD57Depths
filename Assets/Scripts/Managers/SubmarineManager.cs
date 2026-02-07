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

    private HashSet<ushort> _subRecipes = new HashSet<ushort>();
    [SerializeField] private List<SubRecipeSO> _majorUpgrades; // When the player gets this recipe, we increase the "stage"
    
    [SerializeField] private Transform _cutsceneCameraPosUpgradeMachine;
    [SerializeField] private Transform _cutsceneCameraPosControlPanel; 
    [SerializeField] private SpriteRenderer _upgradeMachine; 
    
    private int _upgradeStage;
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

    internal void NewSubUpgrade(SubRecipeSO newUpgrade, SubUpgradeEffect effect) {
        bool success = _subRecipes.Add(newUpgrade.ID);
        if (!success) {
            Debug.LogError("Sub recipe already purchased! Did you assigned a unique ID?");
            return;
        }
        if (_majorUpgrades.Exists(r => r.ID == newUpgrade.ID)) {
            _upgradeStage++;
        }
        HandleCutscene(newUpgrade, effect, ResourceSystem.SubUpgradePanel, _cutsceneCameraPosUpgradeMachine);
        HandleCutscene(newUpgrade, effect, ResourceSystem.SubCables3, _cutsceneCameraPosControlPanel);
    }

    private void HandleCutscene(SubRecipeSO newUpgrade, SubUpgradeEffect effect, ushort ID, Transform cameraPos) {
        if (newUpgrade.ID == ID) {
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

    public int GetUpgradeStage() => _upgradeStage; 
    
    public bool CanMoveTo(int index) {
        return index <= _upgradeStage;
    }

    // FOR DEBUGING PUPROSES!!
    internal void RemoveAllUpgrades() {
        _subRecipes.Clear();
    }
}